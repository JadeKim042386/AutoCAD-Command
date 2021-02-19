using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.PlottingServices;
using System;
using System.Collections.Generic;

[assembly : CommandClass(typeof(WinPlot_Several.IterMake))]

namespace WinPlot_Several
{
    public static class Extensions
    {
        public static Point2d Swap(this Point2d pt, bool flip = true)
        {
            return flip ? new Point2d(pt.Y, pt.X) : pt; //flip이 true면 swap, false면 pt그대로
        }
        
        public static Point3d Pad(this Point2d pt)
        {
            return new Point3d(pt.X, pt.Y, 0); // z 값 0으로 설정
        }
      
        public static Point2d Strip(this Point3d pt)
        {
            return new Point2d(pt.X, pt.Y); //3D Point를 2D Point로 변환
        }
        
        public static ObjectId CreateAndMakeLayoutCurrent(this LayoutManager lm, string name, string number, bool select = true)
        {
            string cur_name = name + number;
            // First try to get the layout
            var id = lm.GetLayoutId(cur_name);

            // If it doesn't exist, we create it
            if (!id.IsValid)
            {
                id = lm.CreateLayout(cur_name);
            }
           
            // And finally we select it
            if (select)
            {
                lm.CurrentLayout = cur_name; //선택한 layout이라면 현재 layout으로 설정
            }
            return id;
        }
        
        public static void ApplyToViewport(this Layout lay, Database db, Transaction tr, int vpNum, Extents2d lo_range, Action<Viewport> f)
        {
            Polyline pl = new Polyline(4);

            pl.AddVertexAt(0, new Point2d(lo_range.MinPoint.X, lo_range.MinPoint.Y), 0, 0, 0);

            pl.AddVertexAt(1, new Point2d(lo_range.MaxPoint.X, lo_range.MinPoint.Y), 0, 0, 0);

            pl.AddVertexAt(2, new Point2d(lo_range.MaxPoint.X, lo_range.MaxPoint.Y), 0, 0, 0);

            pl.AddVertexAt(3, new Point2d(lo_range.MinPoint.X, lo_range.MaxPoint.Y), 0, 0, 0);

            pl.Closed = true;

            //get viewport ids
            var vpIds = lay.GetViewports();

            Viewport vp = null;

            //현재 viewport 찾기
            foreach (ObjectId vpId in vpIds)
            {
                var vp2 = tr.GetObject(vpId, OpenMode.ForWrite) as Viewport;

                if (vp2 != null && vp2.Number == vpNum) //current viewport = 2
                {
                    // We have found our viewport, so call the action
                    vp = vp2;
                    break;
                }
            }

            //viewport가 없으면 만들기
            if (vp != null)
            {
                LayoutManager acLayoutMgr = LayoutManager.Current;

                string currentLo = acLayoutMgr.CurrentLayout;

                DBDictionary LayoutDict = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                Layout CurrentLo = tr.GetObject((ObjectId)LayoutDict[currentLo], OpenMode.ForRead) as Layout;

                BlockTableRecord BlkTblRec = tr.GetObject(CurrentLo.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId ID in BlkTblRec)
                {
                    Viewport VP = tr.GetObject(ID, OpenMode.ForRead) as Viewport;
                    if (VP != null)
                    {
                        VP.UpgradeOpen();
                        VP.Erase();
                    }
                } 
            }

            Entity ent = pl as Entity;

            using (Viewport acVport = new Viewport())
            {
                // Finally we call our function on it
                f(acVport);

                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = tr.GetObject(db.BlockTableId,
                                             OpenMode.ForRead) as BlockTable;

                // Open the Block table record Paper space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.PaperSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                ObjectId id = acBlkTblRec.AppendEntity(ent);
                tr.AddNewlyCreatedDBObject(pl, true);

                // Add the new object to the block table record and the transaction
                acBlkTblRec.AppendEntity(acVport);
                tr.AddNewlyCreatedDBObject(acVport, true);

                // Enable the viewport
                acVport.NonRectClipEntityId = id;

                acVport.NonRectClipOn = true;

                acVport.On = true;

                acVport.Locked = true;
            }
        }

        public static void SetPlotSettings(this Layout lay, string pageSize, string styleSheet, string device)
        {
            using (var ps = new PlotSettings(lay.ModelType))
            {
                ps.CopyFrom(lay);

                var psv = PlotSettingsValidator.Current;

                // Set the device
                var devs = psv.GetPlotDeviceList();

                if (devs.Contains(device))
                {
                    psv.SetPlotConfigurationName(ps, device, null);

                    psv.RefreshLists(ps);
                }
                // Set the media name/size
                var mns = psv.GetCanonicalMediaNameList(ps);

                if (mns.Contains(pageSize))
                {
                    psv.SetCanonicalMediaName(ps, pageSize);
                }
                // Set the pen settings
                var ssl = psv.GetPlotStyleSheetList();

                if (ssl.Contains(styleSheet))
                {
                    psv.SetCurrentStyleSheet(ps, styleSheet);
                }
                // Copy the PlotSettings data back to the Layout
                var upgraded = false;

                if (!lay.IsWriteEnabled)
                {
                    lay.UpgradeOpen(); //Write Open

                    upgraded = true;
                }

                lay.CopyFrom(ps);

                if (upgraded)
                {
                    lay.DowngradeOpen();
                }
            }
        }

        /// Determine the maximum possible size for this layout.
        /// The maximum extents of the viewport on this layout.
        public static Extents2d GetMaximumExtents(this Layout lay)
        {
            // We need to flip the axes if the plot is rotated by 90 or 270 deg
            var doIt = lay.PlotRotation == PlotRotation.Degrees090 || lay.PlotRotation == PlotRotation.Degrees270;

            // Get the extents in the correct units and orientation
            var min = new Point2d(0, 0);

            var max = (lay.PlotPaperSize.Swap(doIt) - lay.PlotPaperMargins.MaxPoint.Swap(doIt).GetAsVector()) - (lay.PlotPaperMargins.MinPoint.Swap(doIt).GetAsVector());

            return new Extents2d(min, max);
        }

        /// Sets the size of the viewport according to the provided extents.
        /// The extents of the viewport on the page.
        /// Optional factor to provide padding.
        public static void ResizeViewport(this Viewport vp, Extents2d ext, double fac = 1.0)
        {
            vp.Width = (ext.MaxPoint.X - ext.MinPoint.X) * fac;
            vp.Height = (ext.MaxPoint.Y - ext.MinPoint.Y) * fac;
            vp.CenterPoint = (Point2d.Origin + (ext.MaxPoint - ext.MinPoint) * 0.5).Pad();
            vp.ViewHeight = (ext.MaxPoint.Y - ext.MinPoint.Y) * fac;
        }

        /// Sets the view in a viewport to contain the specified model extents.
        /// The extents of the content to fit the viewport.
        /// Optional factor to provide padding
        public static void FitContentToViewport(this Viewport vp, Extents3d ext, double fac = 1.0)
        {
            // Let's zoom to just larger than the extents
            vp.ViewCenter = (ext.MinPoint + ((ext.MaxPoint - ext.MinPoint) * 0.5)).Strip();

            // Get the dimensions of our view from the database extents
            //var hgt = ext.MaxPoint.Y - ext.MinPoint.Y;
            //var wid = ext.MaxPoint.X - ext.MinPoint.X;

            // We'll compare with the aspect ratio of the viewport itself
            // (which is derived from the page size)
            //var aspect = vp.Width / vp.Height;

            // If our content is wider than the aspect ratio, make sure we
            // set the proposed height to be larger to accommodate the
            // content
            //if (wid / hgt > aspect)
            //{
            //    hgt = wid / aspect;
            //}

            // Set the height so we're exactly at the extents
            //vp.ViewHeight = hgt;

            // Set a custom scale to zoom out slightly (could also
            // vp.ViewHeight *= 1.1, for instance)
            vp.CustomScale *= fac;
        }
    }
    class CreateLayout
    {
        public void Create_Layout(string lo_name, string number)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            var db = doc.Database;
            var ed = doc.Editor;
            Matrix3d ucs = ed.CurrentUserCoordinateSystem;

            //left-lower point 지정
            PromptPointOptions ppo = new PromptPointOptions("\nSelect first corner of plot area: ");
            ppo.AllowNone = false;
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;
            Point3d first = ppr.Value;

            //corner point 지정
            PromptCornerOptions pco = new PromptCornerOptions("\nSelect second corner of plot area: ", first); //first의 대각선 포인트 지정
            ppr = ed.GetCorner(pco);
            if (ppr.Status != PromptStatus.OK)
                return;
            Point3d second = ppr.Value;

            CoordinateSystem3d cs = ucs.CoordinateSystem3d;

            // Transform from UCS to WCS
            Matrix3d mat = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin,
                Vector3d.XAxis,
                Vector3d.YAxis,
                Vector3d.ZAxis,
                cs.Origin,
                cs.Xaxis,
                cs.Yaxis,
                cs.Zaxis
                );

            Point3d WCS_first = first.TransformBy(mat);
            Point3d WCS_second = second.TransformBy(mat);

            Extents2d ext = new Extents2d();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Create and select a new layout tab
                ObjectId id = LayoutManager.Current.CreateAndMakeLayoutCurrent(lo_name, number);

                // Open the created layout
                Layout lay = tr.GetObject(id, OpenMode.ForWrite) as Layout;

                // Make some settings on the layout and get its extents
                lay.SetPlotSettings("A3", "monochrome.ctb", "Adobe PDF.pc3");

                ext = lay.GetMaximumExtents(); //plot extents2d

                lay.ApplyToViewport(db, tr, 2, ext, vp =>
                {
                    // Size the viewport according to the extents calculated when
                    // we set the PlotSettings (device, page size, etc.)
                    // Use the standard 10% margin around the viewport
                    // (found by measuring pixels on screenshots of Layout1, etc.)

                    vp.ResizeViewport(ext, 1.0); //뷰포트 엔티티 사이즈 조절

                    // Adjust the view so that the model contents fit
                    if (ValidDbExtents(WCS_first, WCS_second))
                    {
                        vp.FitContentToViewport(new Extents3d(WCS_first, WCS_second), 1.0);
                    }
                    // Finally we lock the view to prevent meddling
                    vp.Locked = true;
                }
                );
                // Commit the transaction
                tr.Commit();
            }
            ed.Command("_.ZOOM", "_E");

            ed.Command("_.ZOOM", ".7X");

            ed.Regen();

            Application.SetSystemVariable("TILEMODE", 1);
        }

        private bool ValidDbExtents(Point3d min, Point3d max)
        {
            return !(min.X > 0 && min.Y > 0 && min.Z > 0 && max.X < 0 && max.Y < 0 && max.Z < 0);
        }
    }
    public class PlottingCommands
    {
        public void MultiSheetPlot()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            Application.SetSystemVariable("PUBLISHCOLLATE", 0);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                PlotInfo pi = new PlotInfo();

                PlotInfoValidator piv = new PlotInfoValidator();

                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;

                // A PlotEngine does the actual plotting (can also create one for Preview)
                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        //Layout filtering
                        ObjectIdCollection layoutsToPlot = new ObjectIdCollection();

                        foreach (ObjectId btrId in bt)
                        {
                            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                            if (btr.IsLayout && btr.Name.ToUpper() != BlockTableRecord.ModelSpace.ToUpper())
                            {
                                layoutsToPlot.Add(btrId);
                            }
                        }

                        // Create a Progress Dialog to provide info and allow thej user to cancel                        
                        using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                        {
                            //int numSheet = 1;

                            foreach (ObjectId btrId in layoutsToPlot)
                            {
                                BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                                Layout lo = tr.GetObject(btr.LayoutId, OpenMode.ForRead) as Layout;

                                // We need a PlotSettings object based on the layout settings which we then customize
                                PlotSettings ps = new PlotSettings(lo.ModelType);

                                ps.CopyFrom(lo);

                                // The PlotSettingsValidator helps create a valid PlotSettings object
                                PlotSettingsValidator psv = PlotSettingsValidator.Current;

                                // We'll plot the extents, centered and scaled to fit

                                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);

                                psv.SetUseStandardScale(ps, true);

                                psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);

                                psv.RefreshLists(ps);
                                psv.SetCurrentStyleSheet(ps, "monochrome.ctb");

                                ps.ScaleLineweights = true;

                                ps.ShadePlot = 0;

                                psv.SetPlotConfigurationName(ps, "Adobe PDF.pc3", "A3");

                                // We need a PlotInfo object linked to the layout
                                pi.Layout = btr.LayoutId;

                                // Make the layout we're plotting current
                                LayoutManager.Current.CurrentLayout = lo.LayoutName;

                                // We need to link the PlotInfo to the PlotSettings and then validate it
                                pi.OverrideSettings = ps;

                                piv.Validate(pi);

                                //if (numSheet == 1)
                                //{
                                ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Custom Plot Progress");

                                ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");

                                ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");

                                ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");

                                ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");

                                ppd.LowerPlotProgressRange = 0;

                                ppd.UpperPlotProgressRange = 100;

                                ppd.PlotProgressPos = 0;

                                // Let's start the plot, at last

                                ppd.OnBeginPlot();

                                ppd.IsVisible = true;

                                pe.BeginPlot(ppd, null);

                                // We'll be plotting a single document

                                pe.BeginDocument(pi, doc.Name, null, 1, false, "");
                                //}

                                // Which may contain multiple sheets
                                //ppd.StatusMsgString = "Plotting " + doc.Name.Substring(doc.Name.LastIndexOf("\\") + 1) + " - sheet " + numSheet.ToString() + " of " + layoutsToPlot.Count.ToString();

                                ppd.OnBeginSheet();

                                ppd.LowerSheetProgressRange = 0;

                                ppd.UpperSheetProgressRange = 100;

                                ppd.SheetProgressPos = 0;

                                PlotPageInfo ppi = new PlotPageInfo();

                                pe.BeginPage(ppi, pi, true, null);

                                pe.BeginGenerateGraphics(null);

                                ppd.SheetProgressPos = 50;

                                pe.EndGenerateGraphics(null);

                                // Finish the sheet

                                pe.EndPage(null);

                                ppd.SheetProgressPos = 100;

                                ppd.OnEndSheet();

                                // Finish the document
                                pe.EndDocument(null);

                                //numSheet++;

                                // And finish the plot

                                ppd.PlotProgressPos = 100;

                                ppd.OnEndPlot();

                                pe.EndPlot(null);
                            }
                        }
                    }
                }
                else
                {
                    ed.WriteMessage("\nAnother plot is in progress.");
                }
            }
        }
    }
    class IterMake
    {
        [CommandMethod("itermake")]
        public void Iter_Make()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDic = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead, false) as DBDictionary;

                List<string> lo_lst = new List<string>();

                foreach (DBDictionaryEntry entry in layoutDic)
                {
                    ObjectId layoutId = entry.Value;
                    Layout layout = tr.GetObject(layoutId, OpenMode.ForRead) as Layout;
                    if (layout.LayoutName != "Model" && layout.LayoutName != "Layout1")
                    {
                        lo_lst.Add(layout.LayoutName);
                    }
                }
                if (lo_lst != null)
                {
                    foreach (string layoutName in lo_lst)
                    {
                        LayoutManager.Current.DeleteLayout(layoutName); //Delete layout
                    }
                    tr.Commit();
                }
                else
                {
                    tr.Dispose();
                }
            }

            string res = "";
            int lo_n = 1;

            while (res != "Plot")
            {
                PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
                pKeyOpts.Message = "\nDo you wanna plot? ";
                pKeyOpts.Keywords.Add("Plot");
                pKeyOpts.Keywords.Add("Select");
                pKeyOpts.AllowNone = false;
                PromptResult pKeyRes = doc.Editor.GetKeywords(pKeyOpts);

                if (pKeyRes.Status != PromptStatus.OK)
                    return;

                res = pKeyRes.StringResult;
                if (res == "Select")
                {
                    CreateLayout Create_Layout = new CreateLayout();
                    Create_Layout.Create_Layout("NewLayout", lo_n.ToString());
                    lo_n++;
                }
                else
                {
                    //Delete Layout1
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        DBDictionary layoutDict = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                        foreach (DBDictionaryEntry de in layoutDict)
                        {
                            string layoutName = de.Key;
                            if (layoutName == "Layout1")
                            {
                                LayoutManager.Current.DeleteLayout(layoutName); // Delete layout.
                            }
                        }
                        tr.Commit();
                    }
                    ed.Regen();

                    PlottingCommands excute_plot = new PlottingCommands();
                    excute_plot.MultiSheetPlot();

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        DBDictionary layoutDict = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                        foreach (DBDictionaryEntry de in layoutDict)
                        {
                            string layoutName = de.Key;
                            if (layoutName != "Model" && layoutName != "Layout1")
                            {
                                LayoutManager.Current.DeleteLayout(layoutName); // Delete layout.
                            }
                        }
                        tr.Commit();
                        ed.Regen();
                    }
                }
            }
        }
    }
}