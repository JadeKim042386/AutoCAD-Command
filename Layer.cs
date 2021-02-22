using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

[assembly: CommandClass(typeof(LayerList.LAYER_LL)), CommandClass(typeof(Layiso_LY.LY)),
            CommandClass(typeof(Layiso_LI.LI))]
namespace LayerList
{
    public class LAYER_LL
    {
        [CommandMethod("LL", CommandFlags.Modal)]
        public static void DisplayLayerNames()
        {
            //Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            //Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Open the Layer table for read
                LayerTable lt = trans.GetObject(db.LayerTableId,
                                                OpenMode.ForRead) as LayerTable;

                //Prompt for Text Height
                PromptStringOptions pso = new PromptStringOptions("\nText Height: ");
                pso.AllowSpaces = false;
                PromptResult pr = doc.Editor.GetString(pso);

                if (pr.Status == PromptStatus.Cancel || pr.StringResult == "") return;
              
                int txtH = Int32.Parse(pr.StringResult);
            

                //Prompt for the Get Point
                PromptPointResult ppr;
                PromptPointOptions ppo = new PromptPointOptions("");
                ppo.AllowArbitraryInput = false;
                ppo.AllowNone = true;

                ppo.Message = "\n삽입점 지정: ";
                ppr = doc.Editor.GetPoint(ppo);        

                if (ppr.Status == PromptStatus.Cancel || ppr.Status == PromptStatus.None) return;

                Point3d ispt = ppr.Value;

                //Get the current value from a system variable
                int dimscale = System.Convert.ToInt32(Application.GetSystemVariable("DIMSCALE"));

                foreach (ObjectId objid in lt)
                {
                    LayerTableRecord ltr = trans.GetObject(objid, OpenMode.ForRead) as LayerTableRecord;
                    string LayerName = ltr.Name;

                    TextStyleTable TxtStylTbl = trans.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                    string StylName = "Standard";

                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    DBText text = new DBText();
                    text.SetDatabaseDefaults();
                    text.Position = new Point3d(ispt.X, ispt.Y, 0);
                    text.Height = txtH * dimscale;
                    text.TextString = LayerName;
                    text.Layer = LayerName;
                    text.TextStyleId = TxtStylTbl[StylName];

                    btr.AppendEntity(text);
                    trans.AddNewlyCreatedDBObject(text, true);

                    ispt = new Point3d(ispt.X, ispt.Y - (text.Height * dimscale * 2), 0);
                }
                trans.Commit();
            }
        }
    }
}

namespace Layiso_LY
{
    public class LY
    {
        [CommandMethod("LY", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public static void layiso_ly()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            doc.SendStringToExecute("._layiso S O O ", true, false, false);

        }
    }
}

namespace Layiso_LI
{
    public class LI
    {
        [CommandMethod("LI", CommandFlags.Modal)]
        public static void Layoff_all()
        {
            //Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.SinglePickInSpace = true;

            //Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                PromptSelectionResult psr = ed.GetSelection(pso);

                SelectionSet ss;
                if (psr.Status == PromptStatus.OK)
                {
                    ss = psr.Value;

                    foreach (SelectedObject ssobj in ss)
                    {
                        if (ssobj != null)
                        {
                            Entity ent = trans.GetObject(ssobj.ObjectId,
                                                            OpenMode.ForWrite) as Entity;

                            if (ent != null)
                            {
                                string c_la = ent.Layer;

                                LayerTable lt = trans.GetObject(db.LayerTableId,
                                                OpenMode.ForRead) as LayerTable;

                                foreach (ObjectId objid in lt)
                                {
                                    LayerTableRecord ltr = trans.GetObject(objid, OpenMode.ForWrite) as LayerTableRecord;

                                    if(ltr.Name == c_la)
                                    {
                                        ltr.IsOff = false;
                                    }
                                    else
                                    {
                                        ltr.IsOff = true;
                                    }
                                      
                                }
                            }
                        }
                    }
                }
                trans.Commit();
            }
        }
    }
}