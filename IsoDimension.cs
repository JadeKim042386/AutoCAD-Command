using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System;

[assembly : CommandClass(typeof(IsoDim.IsoTest))]
namespace IsoDim
{
    class IsoTest
    {
        public class LineJigger : EntityJig
        {
            public Point3d mEndPoint = new Point3d();

            public LineJigger(Line ent) : base(ent)
            {
            }

            protected override bool Update()
            {
                (Entity as Line).EndPoint = mEndPoint;

                return true;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\nNext point:");

                prOptions1.BasePoint = (Entity as Line).StartPoint;

                prOptions1.UseBasePoint = true;

                prOptions1.UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.AnyBlankTerminatesInput
                    | UserInputControls.GovernedByOrthoMode | UserInputControls.GovernedByUCSDetect | UserInputControls.UseBasePointElevation
                    | UserInputControls.InitialBlankTerminatesInput | UserInputControls.NullResponseAccepted;

                PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);

                if (prResult1.Status == PromptStatus.Cancel)
                    return SamplerStatus.Cancel;

                if (prResult1.Value.Equals(mEndPoint))
                {
                    return SamplerStatus.NoChange;
                }
                else
                {
                    mEndPoint = prResult1.Value;
                    return SamplerStatus.OK;
                }
            }
        }

        public class DimLineJigger : EntityJig
        {
            public Point3d mEndPoint = new Point3d();
            public Point3d basept = new Point3d();

            public DimLineJigger(Line ent, Point3d BasePt) : base(ent)
            {
                basept = BasePt;
                /*Point3d pt1 = ent.XLine1Point;
                Point3d pt2 = ent.XLine2Point;
                basept = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);*/
            }

            protected override bool Update()
            {
                (Entity as Line).EndPoint = mEndPoint;

                return true;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\nDim Line point:");

                prOptions1.BasePoint = basept;

                prOptions1.UseBasePoint = true;

                prOptions1.UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.AnyBlankTerminatesInput
                    | UserInputControls.GovernedByOrthoMode | UserInputControls.GovernedByUCSDetect | UserInputControls.UseBasePointElevation
                    | UserInputControls.InitialBlankTerminatesInput | UserInputControls.NullResponseAccepted;

                PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);

                if (prResult1.Status == PromptStatus.Cancel)
                    return SamplerStatus.Cancel;

                if (prResult1.Value.Equals(mEndPoint))
                {
                    return SamplerStatus.NoChange;
                }
                else
                {
                    mEndPoint = prResult1.Value;
                    return SamplerStatus.OK;
                }
            }

            public Point3d GetDimLinePoint()
            {
                return mEndPoint;
            }
        } 

        public static TextStyleTable CreateStyle(Transaction tr, Database db)
        {
            TextStyleTable newTextStyleTable = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            
            if (!newTextStyleTable.Has("30DM"))  //The TextStyle is currently not in the database
            {
                newTextStyleTable.UpgradeOpen();
                TextStyleTableRecord newTextStyleTableRecord = new TextStyleTableRecord();
                newTextStyleTableRecord.FileName = "romans.shx";
                newTextStyleTableRecord.Name = "30DM";
                newTextStyleTableRecord.ObliquingAngle = (30 * Math.PI) / 180;
                newTextStyleTable.Add(newTextStyleTableRecord);
                tr.AddNewlyCreatedDBObject(newTextStyleTableRecord, true);
            }

            if (!newTextStyleTable.Has("330DM"))  //The TextStyle is currently not in the database
            {
                newTextStyleTable.UpgradeOpen();
                TextStyleTableRecord newTextStyleTableRecord2 = new TextStyleTableRecord();
                newTextStyleTableRecord2.FileName = "romans.shx";
                newTextStyleTableRecord2.Name = "330DM";
                newTextStyleTableRecord2.ObliquingAngle = (330 * Math.PI) / 180;
                newTextStyleTable.Add(newTextStyleTableRecord2);
                tr.AddNewlyCreatedDBObject(newTextStyleTableRecord2, true);
            }

            return newTextStyleTable;
        }

        [CommandMethod("DM", CommandFlags.Modal)]
        public static void Jig()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                Database db = doc.Database;

                var ppr = ed.GetPoint("\nStart point");
                if (ppr.Status != PromptStatus.OK)
                    return;

                Point3d pt = ppr.Value; //start point
                Line ent = new Line(pt, pt);
                ent.TransformBy(ed.CurrentUserCoordinateSystem); //Transform to UCS

                LineJigger jigger = new LineJigger(ent);
                PromptResult pr = ed.Drag(jigger);

                if (pr.Status == PromptStatus.OK)
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        //Create Text Style
                        TextStyleTable newTextStyleTable = CreateStyle(tr, db);

                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        AlignedDimension acRotDim = new AlignedDimension();
                        acRotDim.XLine1Point = pt;
                        acRotDim.XLine2Point = ent.EndPoint;

                        Point3d pt1 = acRotDim.XLine1Point;
                        Point3d pt2 = acRotDim.XLine2Point;
                        Point3d basept = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);

                        double LineAngle = new Point2d(pt1.X, pt1.Y).GetVectorTo(new Point2d(pt2.X, pt2.Y)).Angle;

                        Line ent2 = new Line(basept, basept);
                        ent2.TransformBy(ed.CurrentUserCoordinateSystem);

                        DimLineJigger Dimjigger = new DimLineJigger(ent2, basept);
                        PromptResult pr2 = ed.Drag(Dimjigger);

                        if (pr2.Status == PromptStatus.OK)
                        {
                            Point3d mEndPoint = Dimjigger.GetDimLinePoint();
                            acRotDim.DimLinePoint = mEndPoint;

                            Point2d base2d = new Point2d(basept.X, basept.Y);
                            Point2d End2d = new Point2d(mEndPoint.X, mEndPoint.Y);
                            int acAng = Convert.ToInt32(base2d.GetVectorTo(End2d).Angle * 180 / Math.PI);

                            acRotDim.SetDatabaseDefaults();

                            if ((pt1.Y == pt2.Y & (acAng == 90 || acAng == 270)))
                            {
                                btr.AppendEntity(acRotDim);
                                tr.AddNewlyCreatedDBObject(acRotDim, true);
                            }
                            else
                            {
                                if (Convert.ToInt32(LineAngle * 180 / Math.PI) == 330 || Convert.ToInt32(LineAngle * 180 / Math.PI) == 150)
                                {
                                    switch (acAng)
                                    {
                                        case 90:
                                            acRotDim.TextStyleId = newTextStyleTable["330DM"];
                                            acRotDim.Oblique = -(60 * Math.PI) / 180;
                                            break;
                                        case 30:
                                            acRotDim.TextStyleId = newTextStyleTable["30DM"];
                                            acRotDim.Oblique = (60 * Math.PI) / 180;
                                            break;
                                        case 270:
                                            acRotDim.TextStyleId = newTextStyleTable["330DM"];
                                            acRotDim.Oblique = -(60 * Math.PI) / 180;
                                            break;
                                        case 210:
                                            acRotDim.TextStyleId = newTextStyleTable["30DM"];
                                            acRotDim.Oblique = (60 * Math.PI) / 180;
                                            break;
                                        default:
                                            acRotDim.Oblique = 0;
                                            break;
                                    }
                                }
                                else if (Convert.ToInt32(LineAngle * 180 / Math.PI) == 30 || Convert.ToInt32(LineAngle * 180 / Math.PI) == 210)
                                {
                                    switch (acAng)
                                    {
                                        case 90:
                                            acRotDim.TextStyleId = newTextStyleTable["30DM"];
                                            acRotDim.Oblique = (60 * Math.PI) / 180;
                                            break;
                                        case 330:
                                            acRotDim.TextStyleId = newTextStyleTable["330DM"];
                                            acRotDim.Oblique = -(60 * Math.PI) / 180;
                                            break;
                                        case 270:
                                            acRotDim.TextStyleId = newTextStyleTable["30DM"];
                                            acRotDim.Oblique = (60 * Math.PI) / 180;
                                            break;
                                        case 150:
                                            acRotDim.TextStyleId = newTextStyleTable["330DM"];
                                            acRotDim.Oblique = -(60 * Math.PI) / 180;
                                            break;
                                        default:
                                            acRotDim.Oblique = 0;
                                            break;
                                    }
                                }
                                else if (Convert.ToInt32(LineAngle * 180 / Math.PI) == 90 || Convert.ToInt32(LineAngle * 180 / Math.PI) == 270)
                                {
                                    switch (acAng)
                                    {
                                        case 30:
                                            acRotDim.TextStyleId = newTextStyleTable["330DM"];
                                            acRotDim.Oblique = -(60 * Math.PI) / 180;
                                            break;
                                        case 330:
                                            acRotDim.TextStyleId = newTextStyleTable["30DM"];
                                            acRotDim.Oblique = (60 * Math.PI) / 180;
                                            break;
                                        case 150:
                                            acRotDim.TextStyleId = newTextStyleTable["30DM"];
                                            acRotDim.Oblique = (60 * Math.PI) / 180;
                                            break;
                                        case 210:
                                            acRotDim.TextStyleId = newTextStyleTable["330DM"];
                                            acRotDim.Oblique = -(60 * Math.PI) / 180;
                                            break;
                                        default:
                                            acRotDim.Oblique = 0;
                                            break;
                                    }
                                }
                                btr.AppendEntity(acRotDim);
                                tr.AddNewlyCreatedDBObject(acRotDim, true);
                            }
                            tr.Commit();
                        }
                    }
                }
                else
                {
                    ent.Dispose();
                    return;
                }
            }
            catch
            {
                return;
            }
        }
    }
}