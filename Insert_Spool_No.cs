using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

[assembly : CommandClass(typeof(LeaderPlacement.LeaderCmds))]

namespace LeaderPlacement
{

    //Create Attribute Block

    class AddRectangleBlock
    {
        public static void AddingRectangleBlock(int dimscale, int prefix_length, string cur_prefix)
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                //Check Block Existence And Create

                if (!acBlkTbl.Has("Spool" + prefix_length.ToString() + dimscale.ToString()))
                {
                    using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                    {
                        acBlkTblRec.Name = "Spool" + prefix_length.ToString() + dimscale.ToString();

                        acBlkTblRec.Origin = new Point3d(0, 0, 0); // Set the insertion point for the block

                        double line_len = 2.5 * prefix_length * dimscale;
                        Line l1 = new Line(new Point3d(-line_len / 2, -2.25 * dimscale, 0), new Point3d(line_len / 2, -2.25 * dimscale, 0));
                        Line l2 = new Line(new Point3d(-line_len / 2, 2.25 * dimscale, 0), new Point3d(line_len / 2, 2.25 * dimscale, 0));

                        Arc a1 = new Arc(new Point3d(-line_len / 2, 0, 0), 2.25 * dimscale, Math.PI * 0.5, 3 * Math.PI * 0.5);
                        Arc a2 = new Arc(new Point3d(line_len / 2, 0, 0), 2.25 * dimscale, 3 * Math.PI * 0.5, Math.PI * 0.5);

                        acBlkTblRec.AppendEntity(l1);
                        acBlkTblRec.AppendEntity(l2);
                        acBlkTblRec.AppendEntity(a1);
                        acBlkTblRec.AppendEntity(a2);

                        // Add an attribute definition to the block

                        using (AttributeDefinition acAttDef = new AttributeDefinition())
                        {
                            acAttDef.Position = new Point3d(0, 0, 0);
                            acAttDef.Verifiable = true;
                            acAttDef.Prompt = "Enter Spool No.: ";
                            acAttDef.Tag = "Spool No.#";
                            acAttDef.TextString = cur_prefix;
                            acAttDef.Height = 2.5 * dimscale;
                            acAttDef.Justify = AttachmentPoint.MiddleCenter;
                            acAttDef.WidthFactor = 0.8;

                            acBlkTblRec.AppendEntity(acAttDef);

                            acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForWrite);
                            acBlkTbl.Add(acBlkTblRec);
                            acTrans.AddNewlyCreatedDBObject(acBlkTblRec, true);
                        }
                    }
                }
                acTrans.Commit();
            }
        }
    }
    public class LeaderCmds
    {

        //Definition LeaderJig

        class DirectionalLeaderJig : EntityJig

        {
            private Point3d _start, _end;

            private int _index;

            private int _lineIndex;

            private bool _started;

            public DirectionalLeaderJig(Point3d start, MLeader ld) : base(ld)

            {
                _start = start; // Start Point

                _end = start; // End Point

                _started = false; // Make sure we don't do this again
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {

                // JigPromptPointOptions Setting

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
                        ml.ContentType = ContentType.BlockContent;

                        _index = ml.AddLeader();
                        _lineIndex = ml.AddLeaderLine(_index);

                        // Set the vertices on the line

                        ml.AddFirstVertex(_lineIndex, _start);
                        ml.AddLastVertex(_lineIndex, _end);

                        _started = true;
                    }
                }
                else
                {
                    ml.Visible = true;

                    // Already have a line, so just set its last vertex
                    
                    ml.SetLastVertex(_lineIndex, _end);

                    ml.EnableDogleg = false;
                    ml.BlockPosition = _end;
                    ml.BlockConnectionType = BlockConnectionType.ConnectBase; // Set Block Insertion Point
                }
                return true;
            }
        }

        [CommandMethod("SPN")]
        public void DirectionalLeader()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            int dimscale = Convert.ToInt32(Application.GetSystemVariable("DIMSCALE"));

            //Attribute Definition Text Sting

            PromptStringOptions stroption = new PromptStringOptions("\nEnter Prefix: ");
            stroption.AllowSpaces = false;
            PromptResult strresult = doc.Editor.GetString(stroption);

            string cur_prefix = strresult.StringResult;
            int prefix_length = cur_prefix.Length;

            if (strresult.Status == PromptStatus.Cancel) return;

            //Enter Start Number

            PromptStringOptions stroption2 = new PromptStringOptions("\nEnter Start Number: ");
            stroption.AllowSpaces = false;
            PromptResult strresult2 = doc.Editor.GetString(stroption2);

            if (strresult2.Status == PromptStatus.Cancel) return;
            int start_no = Convert.ToInt32(strresult2.StringResult);

            

            List<int> num_list = new List<int>();

            while (true)
            {
                // Check Number

                if (!Find_obj(cur_prefix)) return;

                //Click Start Point

                var ppr = ed.GetPoint("\nStart point of leader");

                if (ppr.Status != PromptStatus.OK) return;

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;

                    var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false) as BlockTableRecord;

                    //Add Block

                    AddRectangleBlock.AddingRectangleBlock(dimscale, prefix_length, cur_prefix);

                    // Create Leader

                    var ml = new MLeader();
                    ml.Visible = false;
                    ml.BlockContentId = bt["Spool" + prefix_length.ToString() + dimscale.ToString()];
                    ml.ArrowSize = 4 * dimscale;

                    // Set XData

                    SetXData_Template.SetXData_Template.AddRegAppTableRecord("Jade");

                    if (num_list.Count() == 0)
                    {
                        ResultBuffer rb = new ResultBuffer(new TypedValue(1001, "Jade"), new TypedValue(1000, cur_prefix), new TypedValue(1000, start_no.ToString()));
                        ml.XData = rb;
                        rb.Dispose();
                    }
                    else
                    {
                        ResultBuffer rb = new ResultBuffer(new TypedValue(1001, "Jade"), new TypedValue(1000, cur_prefix), new TypedValue(1000, (num_list.Max() + 1).ToString()));
                        ml.XData = rb;
                        rb.Dispose();
                    }

                    // Create jig

                    var jig = new DirectionalLeaderJig(ppr.Value, ml);

                    // Leader's Block Attribute Definition Setting

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

                            if (num_list.Count() == 0)
                            {
                                AttributeRef.TextString = cur_prefix + start_no.ToString();
                                num_list.Add(start_no);
                            }
                            else
                            {
                                int nxt_n = num_list.Max() + 1;
                                AttributeRef.TextString = cur_prefix + nxt_n.ToString();
                                num_list.Add(nxt_n);
                            }
                            
                            ml.SetBlockAttribute(blkEntId, AttributeRef);
                        }
                    }

                    btr.AppendEntity(ml);
                    tr.AddNewlyCreatedDBObject(ml, true);

                    var res = ed.Drag(jig);

                    if (res.Status == PromptStatus.OK)
                    {
                        tr.Commit();
                    }
                }
            }
        }

        //Check Spool No. Duplication

        public static bool Find_obj(string prefix)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false) as BlockTableRecord;

                List<object> arr = new List<object>();

                bool check_prefix = false;

                foreach (ObjectId objid in btr)
                {
                    if (objid.ObjectClass.DxfName == "MULTILEADER")
                    {
                        MLeader cur_leader = tr.GetObject(objid, OpenMode.ForRead) as MLeader;

                        ResultBuffer rb = cur_leader.XData;

                        if (rb == null)
                        {
                            ed.WriteMessage("\nEntity does not have XData attached.");
                        }
                        else
                        {
                            List<object> cur_data = new List<object>();

                            foreach (TypedValue tv in rb)
                            {
                                cur_data.Add(tv.Value);
                            }

                            //Check prefix
                            if (cur_data[1].ToString() == prefix)
                            {
                                check_prefix = true;
                            }
                            else
                            {
                                check_prefix = false;
                            }

                            //Check Count and Contains
                            if (arr.Contains(cur_data[2]) && check_prefix)
                            {
                                Application.ShowAlertDialog(cur_data[2].ToString() + " is duplicate!! Check this!!");
                                return false;
                            }
                            else
                            {
                                arr.Add(cur_data[2]);
                            }
                            rb.Dispose();
                        }
                    }
                }
                return true;
            }
        }
    }
}