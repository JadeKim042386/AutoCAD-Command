using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Command_Setting;

[assembly : CommandClass(typeof(LeaderPlacement.LeaderCmds))]

namespace LeaderPlacement
{
    class AddRectangleBlcok
    {
        public static void AddingRectangleBlock()
        {
            // Get the current database and start a transaction
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            //get dimscale
            int dimscale = System.Convert.ToInt32(Application.GetSystemVariable("DIMSCALE"));

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!acBlkTbl.Has("RecNumber" + dimscale))
                {
                    using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                    {
                        acBlkTblRec.Name = "RecNumber" + dimscale;

                        // Set the insertion point for the block
                        acBlkTblRec.Origin = new Point3d(0, 0, 0);

                        // Add a circle to the block
                        using (Polyline pl = new Polyline(4))
                        {
                            pl.AddVertexAt(0, new Point2d(-1.822 * dimscale, -1.822 * dimscale), 0, 0, 0);

                            pl.AddVertexAt(1, new Point2d(1.822 * dimscale, -1.822 * dimscale), 0, 0, 0);

                            pl.AddVertexAt(2, new Point2d(1.822 * dimscale, 1.822 * dimscale), 0, 0, 0);

                            pl.AddVertexAt(3, new Point2d(-1.822 * dimscale, 1.822 * dimscale), 0, 0, 0);

                            pl.Closed = true;

                            acBlkTblRec.AppendEntity(pl);

                            // Add an attribute definition to the block
                            using (AttributeDefinition acAttDef = new AttributeDefinition())
                            {
                                acAttDef.Position = new Point3d(0, 0, 0);
                                acAttDef.Verifiable = true;
                                acAttDef.Prompt = "Enter Number: ";
                                acAttDef.Tag = "Number#";
                                acAttDef.TextString = "00";
                                acAttDef.Height = 1.8 * dimscale;
                                acAttDef.Justify = AttachmentPoint.MiddleCenter;

                                acBlkTblRec.AppendEntity(acAttDef);

                                acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForWrite);
                                acBlkTbl.Add(acBlkTblRec);
                                acTrans.AddNewlyCreatedDBObject(acBlkTblRec, true);
                            }
                        }
                    }
                }

                // Save the new object to the database
                acTrans.Commit();

                // Dispose of the transaction
            }
        }
    }
    public class LeaderCmds
    {
        class DirectionalLeaderJig : EntityJig

        {
            private Point3d _start, _end;

            private int _index;

            private int _lineIndex;

            private bool _started;

            public DirectionalLeaderJig(Point3d start, MLeader ld) : base(ld)

            {
                // Store info that's passed in, but don't init the MLeader

                /*_InsertionPoint = blockinsertionpoint;*/

                _start = start; //시작점

                _end = start; //끝점

                _started = false;
            }

            // A fairly standard Sampler function
            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                var po = new JigPromptPointOptions();

                po.UserInputControls = (UserInputControls.Accept3dCoordinates | UserInputControls.NoNegativeResponseAccepted);

                po.Message = "\nEnd point";

                var res = prompts.AcquirePoint(po);

                if (_end == res.Value)
                {
                    return SamplerStatus.NoChange;
                }
                else if (res.Status == PromptStatus.OK)
                {
                    _end = res.Value;

                    return SamplerStatus.OK;
                }
                return SamplerStatus.Cancel;
            }

            protected override bool Update()
            {
                var ml = Entity as MLeader;

                if (!_started)
                {
                    if (_start.DistanceTo(_end) > Tolerance.Global.EqualPoint)
                    {
                        // When the jig actually starts - and we have mouse movement -
                        // we create the MText and init the MLeader
                        ml.ContentType = ContentType.BlockContent;

                        /*var mt = new MText();

                        mt.Contents = _contents;

                        ml.MText = mt;*/

                        // Create the MLeader cluster and add a line to it
                        _index = ml.AddLeader();

                        _lineIndex = ml.AddLeaderLine(_index);

                        // Set the vertices on the line
                        ml.AddFirstVertex(_lineIndex, _start);

                        ml.AddLastVertex(_lineIndex, _end);

                        // Make sure we don't do this again
                        _started = true;
                    }
                }
                else
                {
                    // We only make the MLeader visible on the second time through
                    // (this also helps avoid some strange geometry flicker)
                    ml.Visible = true;

                    // We already have a line, so just set its last vertex
                    ml.SetLastVertex(_lineIndex, _end);
                }
                if (_started)
                {
                    // Set the direction of the text to depend on the X of the end-point
                    // (i.e. is if to the left or right of the start-point?)
                    /*var dl = new Vector3d((_end.X >= _start.X ? 1 : -1), 0, 0);

                    ml.SetDogleg(_index, dl);*/

                    ml.EnableDogleg = false;

                    ml.BlockPosition = _end; //block 위치 지정
                    ml.BlockConnectionType = BlockConnectionType.ConnectBase; //block 삽입을 insertion point로 지정
                }
                return true;
            }
        }

        [CommandMethod("MLNUMBER")]
        public void DirectionalLeader()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            var ed = doc.Editor;

            var db = doc.Database;

            //get dimscale
            int dimscale = System.Convert.ToInt32(Application.GetSystemVariable("DIMSCALE"));

            //Kind of Block
            var circle = false;

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\nWhat Kind of Block? ";
            pKeyOpts.Keywords.Add("Circle");
            pKeyOpts.Keywords.Add("Rectangle");
            pKeyOpts.AllowNone = false;
            PromptResult pKeyRes = doc.Editor.GetKeywords(pKeyOpts);

            if (pKeyRes.Status != PromptStatus.OK)
                return;

            if (pKeyRes.StringResult == "Circle")
            { circle = true; }

            /*//Block Insertion Point
            PromptPointResult pointresult;
            PromptPointOptions pointoption = new PromptPointOptions("\nInsertion Point");

            pointresult = doc.Editor.GetPoint(pointoption);
            Point3d blockinsertionpoint = pointresult.Value;

            if (pointresult.Status == PromptStatus.Cancel) return;*/

            /*// Ask the user for the string and the start point of the leader
            var pso = new PromptStringOptions("\nEnter text");

            pso.AllowSpaces = false;

            var pr = ed.GetString(pso);

            if (pr.Status != PromptStatus.OK)
                return;*/

            while (true)
            {
                //Get Circle Number
                PromptStringOptions stroption = new PromptStringOptions("\nEnter Current Number: ");
                stroption.AllowSpaces = false;
                PromptResult strresult = doc.Editor.GetString(stroption);

                string circle_number = strresult.StringResult;

                if (strresult.Status == PromptStatus.Cancel) return;

                //Start Point
                var ppr = ed.GetPoint("\nStart point of leader");

                if (ppr.Status != PromptStatus.OK)
                    return;

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;

                    var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false) as BlockTableRecord;

                    if (circle)
                    {
                        //"CurcleNumber" 블록이 없을때 예외처리
                        if (!bt.Has("CircleNumber" + dimscale))
                        {
                            AddAttributeBlock addblock = new AddAttributeBlock();
                            addblock.AddingAttributeBlock();
                        }
                    }
                    else
                    {
                        //"RecNumber" 블록이 없을때 예외처리
                        if (!bt.Has("RecNumber" + dimscale))
                        {
                            AddRectangleBlcok.AddingRectangleBlock();
                        }
                    }

                    // Create and pass in an invisible MLeader
                    // This helps avoid flickering when we start the jig
                    var ml = new MLeader();

                    ml.Visible = false;

                    if (circle)
                    {
                        ml.BlockContentId = bt["CircleNumber" + dimscale];
                    }
                    else
                    {
                        ml.BlockContentId = bt["RecNumber" + dimscale];
                    }

                    ml.ArrowSize = 4 * dimscale;

                    // Create jig
                    var jig = new DirectionalLeaderJig(ppr.Value, ml);

                    //지정된 block에 접근
                    BlockTableRecord blkLeader = tr.GetObject(ml.BlockContentId, OpenMode.ForRead) as BlockTableRecord;

                    Matrix3d transfo = Matrix3d.Displacement(ml.BlockPosition.GetAsVector());

                    foreach (ObjectId blkEntId in blkLeader)
                    {
                        AttributeDefinition AttributeDef = tr.GetObject(blkEntId, OpenMode.ForRead) as AttributeDefinition;

                        if (AttributeDef != null)
                        {
                            AttributeReference AttributeRef = new AttributeReference();

                            AttributeRef.SetAttributeFromBlock(AttributeDef, transfo);

                            AttributeRef.Position = AttributeDef.Position.TransformBy(transfo);

                            AttributeRef.TextString = circle_number;

                            ml.SetBlockAttribute(blkEntId, AttributeRef);
                        }
                    }

                    // Add the MLeader to the drawing: this allows it to be displayed
                    btr.AppendEntity(ml);

                    tr.AddNewlyCreatedDBObject(ml, true);

                    // Set end point in the jig
                    var res = ed.Drag(jig);

                    // If all is well, commit
                    if (res.Status == PromptStatus.OK)
                    {
                        tr.Commit();
                    }
                }
            }
        }
    }
}