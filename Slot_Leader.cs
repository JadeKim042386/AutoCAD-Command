using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;

[assembly : CommandClass(typeof(Slot_Leader.Slot_Leader))]

namespace Slot_Leader
{
    class Slot_Leader
    {
        //Create Block And Text
        static void AddingBlockAndText(int dimscale, int prefix_length)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!acBlkTbl.Has("SlotNumber" + dimscale))
                {
                    using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                    {
                        acBlkTblRec.Name = "SlotNumber" + dimscale;

                        // Set the insertion point for the block
                        acBlkTblRec.Origin = new Point3d(0, 0, 0);

                        double line_len = (prefix_length + 16) * dimscale; //Text Length x Dimscale
                        Line l1 = new Line(new Point3d(-line_len / 2, -2.25 * dimscale, 0), new Point3d(line_len / 2, -2.25 * dimscale, 0));
                        Line l2 = new Line(new Point3d(-line_len / 2, 2.25 * dimscale, 0), new Point3d(line_len / 2, 2.25 * dimscale, 0));

                        Arc a1 = new Arc(new Point3d(-line_len / 2, 0, 0), 2.25 * dimscale, Math.PI * 0.5, 3 * Math.PI * 0.5);
                        Arc a2 = new Arc(new Point3d(line_len / 2, 0, 0), 2.25 * dimscale, 3 * Math.PI * 0.5, Math.PI * 0.5);

                        acBlkTblRec.AppendEntity(l1);
                        acBlkTblRec.AppendEntity(l2);
                        acBlkTblRec.AppendEntity(a1);
                        acBlkTblRec.AppendEntity(a2);

                        acBlkTbl.UpgradeOpen();
                        acBlkTbl.Add(acBlkTblRec);
                        acTrans.AddNewlyCreatedDBObject(acBlkTblRec, true);
                    }
                }

                // Save the new object to the database
                acTrans.Commit();
            }
        }

        // Jig
        public class DirectionalLeaderJig : EntityJig
        {
            private Point3d _start, _end;
            private int _index;
            private int _lineIndex;
            private bool _started;

            public DirectionalLeaderJig(Point3d start, MLeader ld) : base(ld)
            {
                _start = start;
                _end = start;

                _started = false;
            }

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
                var ml = (MLeader)Entity;

                if (!_started)
                {
                    if (_start.DistanceTo(_end) > Tolerance.Global.EqualPoint)
                    {
                        ml.ContentType = ContentType.BlockContent;

                        _index = ml.AddLeader();
                        _lineIndex = ml.AddLeaderLine(_index);

                        ml.AddFirstVertex(_lineIndex, _start);
                        ml.AddLastVertex(_lineIndex, _end);

                        _started = true;
                    }
                }
                else
                {
                    ml.Visible = true;
                    ml.SetLastVertex(_lineIndex, _end);
                }

                if (_started)
                {
                    // Set the direction of the text to depend on the X of the end-point

                    var dl = new Vector3d((_end.X >= _start.X ? 1 : -1), 0, 0);
                    ml.SetDogleg(_index, dl);
                }
                return true;
            }
        }

        [CommandMethod("TEST")]
        public static void DirectionalLeader()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            int dimscale = Convert.ToInt32(Application.GetSystemVariable("DIMSCALE"));

            var pso = new PromptStringOptions("\nEnter Prefix");
            pso.AllowSpaces = false;
            var pr = ed.GetString(pso);

            if (pr.Status != PromptStatus.OK)
                return;

            while (true)
            {
                var ppr = ed.GetPoint("\nStart point of leader");
                if (ppr.Status != PromptStatus.OK)
                    return;

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false);

                    var ml = new MLeader();
                    ml.Visible = false;
                    ml.EnableDogleg = true;
                    ml.DoglegLength = 3.5 * dimscale;
                    ml.Scale = dimscale;

                    var jig = new DirectionalLeaderJig(ppr.Value, ml);

                    btr.AppendEntity(ml);
                    tr.AddNewlyCreatedDBObject(ml, true);

                    var res = ed.Drag(jig);

                    if (res.Status == PromptStatus.OK)
                    {
                        var endpt = ml.GetLastVertex(0); //Leader EndPoint
                        var vec = ml.GetDogleg(0); //Leader's Dogleg Vector Value

                        //Adding Block
                        AddingBlockAndText(dimscale, pr.StringResult.Length);
                        var blkid = bt["SlotNumber" + dimscale];

                        //Calculating Block Length
                        double line_len = (pr.StringResult.Length + 16) * dimscale;

                        //Calculating Point for Insert Block
                        var Inst_pt = new Point3d(0, 0, 0);
                        if (vec == new Vector3d(1, 0, 0))
                        {
                            Inst_pt = new Point3d(endpt.X + ((ml.GetDoglegLength(0) + 2.25) * dimscale) + (line_len / 2), endpt.Y, endpt.Z);
                        }
                        else
                        {
                            Inst_pt = new Point3d(endpt.X - ((ml.GetDoglegLength(0) + 2.25) * dimscale) - (line_len / 2), endpt.Y, endpt.Z);
                        }

                        //Insert Block at Inst_pt
                        using (BlockReference acBlkRef = new BlockReference(Inst_pt, blkid))
                        {
                            btr.AppendEntity(acBlkRef);
                            tr.AddNewlyCreatedDBObject(acBlkRef, true);
                        }

                        using (DBText txt = new DBText())
                        {
                            txt.TextString = pr.StringResult + "000";
                            txt.WidthFactor = 0.8;
                            txt.Height = 2.5 * dimscale;
                            txt.Justify = AttachmentPoint.MiddleCenter;
                            txt.AlignmentPoint = Inst_pt;

                            btr.AppendEntity(txt);
                            tr.AddNewlyCreatedDBObject(txt, true);
                        }

                        tr.Commit();
                    }
                }
            }        
        }
    }
}
