using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.PlottingServices;
using System.Runtime.InteropServices;

[assembly: CommandClass(typeof(PlottingApplication.SimplePlottingCommands))]

namespace PlottingApplication
{
    public class SimplePlottingCommands
    {
        //acad.exe에서 acedTrans 함수 호출
        [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]

        //한 좌표계에서 다른 좌표계로 점 또는 변위를 변환
        static extern int acedTrans(
          double[] point, //변환할 포인트
          IntPtr fromRb, //변환할 좌표계
          IntPtr toRb, //변환 결과가 되는 좌표계
          int disp, //0이아니면 point가 변위 벡터로 처리됨
          double[] result //결과값
        );

        static public Extents2d UCSToDCS (Point3d first, Point3d second)
        {
            // Transform from UCS to DCS(Drawing Coordinate System)
            //ResultBuffer = data types의 structure
            //codevalue = 5003 = RTSHORT
            ResultBuffer rbFrom = new ResultBuffer(new TypedValue(5003, 1)), //1 = UCS
                        rbTo = new ResultBuffer(new TypedValue(5003, 2)); //2 = DCS

            double[] firres = new double[] { 0, 0, 0 };
            double[] secres = new double[] { 0, 0, 0 };

            //각각 first, second가 DCS로 변환되며 firres, secres로 output됨
            // Transform the first point...
            acedTrans(
              first.ToArray(), //첫번째 포인트를 배열로 변환
              rbFrom.UnmanagedObject, //Unmanaged Pointer에 접근??
              rbTo.UnmanagedObject,
              0,
              firres
            );

            // ... and the second
            acedTrans(
              second.ToArray(),
              rbFrom.UnmanagedObject,
              rbTo.UnmanagedObject,
              0,
              secres
            );

            // We can safely drop the Z-coord at this stage
            //Extents2d = 2d 범위를 구현
            Extents2d window = new Extents2d(firres[0], firres[1], secres[0], secres[1]);

            return window;
        }

        [CommandMethod("Winplot")]
        static public void WindowPlot()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            Application.SetSystemVariable("BACKGROUNDPLOT", 2);

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

            Extents2d window = UCSToDCS(first, second);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // We'll be plotting the current layout
                BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                Layout lo = tr.GetObject(btr.LayoutId, OpenMode.ForRead) as Layout;

                // We need a PlotInfo object
                // linked to the layout

                //PlotInfo Object생성
                PlotInfo pi = new PlotInfo();

                //PlotInfo에 현재 Layout 설정
                pi.Layout = btr.LayoutId;

                // We need a PlotSettings object based on the layout settings which we then customize

                //Plotsettings Object를 생성하여 현재 Layout의 ModelType을 지정
                PlotSettings ps = new PlotSettings(lo.ModelType);
                ps.CopyFrom(lo); //Layout의 값들을 복사

                // The PlotSettingsValidator helps create a valid PlotSettings object
                PlotSettingsValidator psv = PlotSettingsValidator.Current; //현재 Item에 accsss

                // We'll plot the extents, centered and scaled to fit
                psv.SetPlotWindowArea(ps, window); //인쇄 영역 지정 (plot type을 window로 지정해야함)

                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window); //plot type을 window로 지정

                psv.SetUseStandardScale(ps, true); //false면 custom scale 지정해야함

                psv.SetStdScaleType(ps, StdScaleType.ScaleToFit); //용지에 맞춤으로 지정

                psv.SetPlotCentered(ps, true); //plot의 중심으로 지정

                psv.RefreshLists(ps);
                psv.SetCurrentStyleSheet(ps, "monochrome.ctb");

                ps.ScaleLineweights = true;

                ps.ShadePlot = 0;

                // We'll use the Adobe PDF, as for today we're just plotting to file
                psv.SetPlotConfigurationName(ps, "Adobe PDF", "A3");

                // We need to link the PlotInfo to the PlotSettings and then validate it
                pi.OverrideSettings = ps; //Plot Info의 Plot Settings를 지정

                PlotInfoValidator piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabledCustom; //media가 일치하는지 확인하는 Validator생성
                piv.Validate(pi); //유효성 검사

                // A PlotEngine does the actual plotting(can also create one for Preview)
                //PlotFactory = Plot Engine을 생성
                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting || PlotFactory.ProcessPlotState == ProcessPlotState.ForegroundPlotting) //현재 plot이 진행되고 있지 않다면
                {
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        // Create a Progress Dialog to provide info and allow thej user to cancel
                        using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                        {
                            ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Custom Plot Progress");

                            ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");

                            ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");

                            ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");

                            ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");

                            //plot 진행률 설정
                            ppd.LowerPlotProgressRange = 0;
                            ppd.UpperPlotProgressRange = 100;
                            ppd.PlotProgressPos = 0;

                            // Let's start the plot, at last
                            ppd.OnBeginPlot(); //플롯 시작 알림이 성공했는지 여부, End 필요

                            ppd.IsVisible = true;

                            pe.BeginPlot(ppd, null); //플롯 시작, End 필요

                            pe.BeginDocument(pi, doc.Name, null, 1, false, ""); //End 필요

                            // Which contains a single sheet
                            ppd.OnBeginSheet(); //End 필요

                            //진행율을 0 ~ 100으로 지정
                            ppd.LowerSheetProgressRange = 0;
                            ppd.UpperSheetProgressRange = 100;
                            ppd.SheetProgressPos = 0;

                            PlotPageInfo ppi = new PlotPageInfo();

                            pe.BeginPage(ppi, pi, true, null); //End 필요
                            pe.BeginGenerateGraphics(null);
                            pe.EndGenerateGraphics(null);

                            // Finish the sheet
                            pe.EndPage(null);
                            ppd.SheetProgressPos = 100;
                            ppd.OnEndSheet();

                            // Finish the document
                            pe.EndDocument(null);

                            // And finish the plot
                            ppd.PlotProgressPos = 100;
                            ppd.OnEndPlot();
                            pe.EndPlot(null);
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
}

