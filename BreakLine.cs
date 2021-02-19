using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly : CommandClass(typeof(BreakLine.BreakLine))]

namespace BreakLine
{
    class GetLine
    {
        public Line FirstLine { get; set; }
        public Polyline FirstPolyline { get; set; }
        public Line SecondLine { get; set; }
        public Polyline SecondPolyline { get; set; }
        public Point3dCollection pts { get; set; } = new Point3dCollection();
        public DBObjectCollection objs { get; set; }
    }
    class SaveId
    {
        public ObjectId[] ids { get; set; }
    }
    public class BreakLine
    {
        [CommandMethod("Q3")]
        public static void Q2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            int dimscale = System.Convert.ToInt32(Application.GetSystemVariable("DIMSCALE"));

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                PromptStringOptions CircleSize = new PromptStringOptions("\nEnter Break Length: ");
                CircleSize.AllowSpaces = false;
                PromptResult strresult = doc.Editor.GetString(CircleSize);
                if (strresult.Status != PromptStatus.OK)
                    return;

                int circlesize = System.Convert.ToInt32(strresult.StringResult);

                TypedValue[] acTypValAr = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Operator, "<or"),
                    new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                    new TypedValue((int)DxfCode.Start, "POLYLINE"),
                    new TypedValue((int)DxfCode.Start, "LINE"),
                    new TypedValue((int)DxfCode.Operator, "or>")
                 };

                SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
                PromptSelectionResult acSSPrompt = ed.GetSelection(acSelFtr);

                SaveId ids = new SaveId();

                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    ids.ids = acSSPrompt.Value.GetObjectIds();
                }
                else
                {
                    return;
                }

               /* PromptEntityOptions peo = new PromptEntityOptions("Select first line: ");
                peo.SetRejectMessage("\nYou have to select polyline or line...>>");
                peo.AddAllowedClass(typeof(Polyline), false);
                peo.AddAllowedClass(typeof(Line), false);
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                    return;*/

                //Get second line
                PromptEntityOptions peo2 = new PromptEntityOptions("\nSelect second line: ");
                peo2.SetRejectMessage("\nYou have to select polyline or line...>>");
                peo2.AddAllowedClass(typeof(Polyline), false);
                peo2.AddAllowedClass(typeof(Line), false);
                PromptEntityResult per2 = ed.GetEntity(peo2);
                if (per2.Status != PromptStatus.OK)
                    return;

                Entity ent2 = trans.GetObject(per2.ObjectId, OpenMode.ForRead) as Entity;
                if (ent2 == null)
                    return;

                foreach (ObjectId id in ids.ids) //FirstLine
                {
                    Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null)
                        return;

                    GetLine line = new GetLine();

                    if (ent.GetType() == typeof(Polyline) && ent2.GetType() == typeof(Polyline))
                    {
                        line.FirstPolyline = ent as Polyline;
                        line.SecondPolyline = ent2 as Polyline;

                        //FirstLine and SecondLine Intersection Point
                        line.FirstPolyline.IntersectWith(line.SecondPolyline, Intersect.OnBothOperands, line.pts, IntPtr.Zero, IntPtr.Zero);
                    }
                    else if (ent.GetType() == typeof(Polyline) && ent2.GetType() == typeof(Line))
                    {
                        line.FirstPolyline = ent as Polyline;
                        line.SecondLine = ent2 as Line;
                        //FirstLine and SecondLine Intersection Point
                        line.FirstPolyline.IntersectWith(line.SecondLine, Intersect.OnBothOperands, line.pts, IntPtr.Zero, IntPtr.Zero);
                    }
                    else if (ent.GetType() == typeof(Line) && ent2.GetType() == typeof(Polyline))
                    {
                        line.FirstLine = ent as Line;
                        line.SecondPolyline = ent2 as Polyline;
                        //FirstLine and SecondLine Intersection Point
                        line.FirstLine.IntersectWith(line.SecondPolyline, Intersect.OnBothOperands, line.pts, IntPtr.Zero, IntPtr.Zero);
                    }
                    else if (ent.GetType() == typeof(Line) && ent2.GetType() == typeof(Line))
                    {
                        line.FirstLine = ent as Line;
                        line.SecondLine = ent2 as Line;
                        //FirstLine and SecondLine Intersection Point
                        line.FirstLine.IntersectWith(line.SecondLine, Intersect.OnBothOperands, line.pts, IntPtr.Zero, IntPtr.Zero);
                    }

                    //FirstLine and Circle Intersection Point
                    Point3dCollection Circle_pts = new Point3dCollection();

                    foreach (Point3d pt in line.pts)
                    {
                        using (Transaction acTrans = db.TransactionManager.StartTransaction())
                        {
                            BlockTable acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                            using (Circle acCirc = new Circle())
                            {
                                acCirc.Center = pt;
                                acCirc.Radius = circlesize * dimscale;

                                // Add the new object to the block table record and the transaction
                                acBlkTblRec.AppendEntity(acCirc);
                                acTrans.AddNewlyCreatedDBObject(acCirc, true);

                                if (line.FirstPolyline != null)
                                {
                                    line.FirstPolyline.IntersectWith(acCirc, Intersect.OnBothOperands, Circle_pts, IntPtr.Zero, IntPtr.Zero);
                                }
                                else
                                {
                                    line.FirstLine.IntersectWith(acCirc, Intersect.OnBothOperands, Circle_pts, IntPtr.Zero, IntPtr.Zero);
                                }
                            }
                        }

                        //Circle and FirstLine Intersection Point
                        List<double> pars = new List<double>();
                        foreach (Point3d Circle_pt in Circle_pts)
                        {
                            if (line.FirstPolyline != null)
                            {
                                pars.Add(line.FirstPolyline.GetParameterAtPoint(Circle_pt));
                            }
                            else
                            {
                                pars.Add(line.FirstLine.GetParameterAtPoint(Circle_pt));
                            }
                        }

                        for (int i = 0; i < pars.Count; i += 2)
                        {
                            Point3d pt1 = Circle_pts[i];
                            Point3d pt2 = Circle_pts[i + 1];

                            List<double> Circle_pars = new List<double>();
                            Circle_pars.Add(pars[i]);
                            Circle_pars.Add(pars[i + 1]);
                            Circle_pars.Sort();

                            if (line.FirstPolyline != null)
                            {
                                line.objs = line.FirstPolyline.GetSplitCurves(new DoubleCollection(Circle_pars.ToArray()));
                                foreach (Polyline pl in line.objs)
                                {
                                    if ((pl.StartPoint != pt1 && pl.StartPoint != pt2) ^ (pl.EndPoint != pt1 && pl.EndPoint != pt2))
                                    {
                                        btr.AppendEntity(pl);
                                        trans.AddNewlyCreatedDBObject(pl, true);
                                    }
                                }
                            }
                            else
                            {
                                line.objs = line.FirstLine.GetSplitCurves(new DoubleCollection(Circle_pars.ToArray()));
                                foreach (Line l in line.objs)
                                {
                                    if ((l.StartPoint != pt1 && l.StartPoint != pt2) ^ (l.EndPoint != pt1 && l.EndPoint != pt2))
                                    {
                                        btr.AppendEntity(l);
                                        trans.AddNewlyCreatedDBObject(l, true);
                                    }
                                }
                            }
                        }
                    }
                    if (line.FirstPolyline != null)
                    {
                        line.FirstPolyline.UpgradeOpen();
                        line.FirstPolyline.Erase();
                    }
                    else
                    {
                        line.FirstLine.UpgradeOpen();
                        line.FirstLine.Erase();
                    }
                }
                trans.Commit();
            }
        }
    }
}
