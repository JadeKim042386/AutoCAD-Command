using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

[assembly : CommandClass(typeof(TextTotalSum.TextTotalSum))]

namespace TextTotalSum
{
    
    public class TextTotalSum
    {
        [CommandMethod("Textsum")]
        public static void FilterMtextWildcard()
        {
            // Get the current document editor
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acDb = acDoc.Database;
            Editor acEd = acDoc.Editor;

            var text = false;
            var mtext = false;
            var dim = false;

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\nWhat Kind of Text? ";
            pKeyOpts.Keywords.Add("DText");
            pKeyOpts.Keywords.Add("MText");
            pKeyOpts.Keywords.Add("Dimension");
            pKeyOpts.AllowNone = false;
            PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);

            if (pKeyRes.Status != PromptStatus.OK)
                return;

            if (pKeyRes.StringResult == "DText")
            { text = true; }
            else if (pKeyRes.StringResult == "MText")
            { mtext = true; }
            else
            { dim = true; }

            double Result = 0;

            using (Transaction acTrans = acDb.TransactionManager.StartTransaction())
            {
                TypedValue[] acTypValAr = new TypedValue[1];
                if (text)
                {
                    acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "TEXT"), 0);
                }
                else if (mtext)
                {
                    acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "MTEXT"), 0);
                }
                else
                {
                    acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "Dimension"), 0);
                }
                
                // Assign the filter criteria to a SelectionFilter object
                SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);

                // Request for objects to be selected in the drawing area
                PromptSelectionResult acSSPrompt;
                acSSPrompt = acEd.GetSelection(acSelFtr);

                // If the prompt status is OK, objects were selected
                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                    foreach (ObjectId id in ids)
                    {
                        if (id == null)
                            return;

                        if (text)
                        {
                            DBText acEnt = acTrans.GetObject(id, OpenMode.ForWrite) as DBText;
                            Result += System.Convert.ToDouble(acEnt.TextString);
                        }
                        else if (mtext)
                        {
                            MText acEnt = acTrans.GetObject(id, OpenMode.ForWrite) as MText;
                            Result += System.Convert.ToDouble(acEnt.Text);
                        }
                        else
                        {
                            Dimension acEnt = acTrans.GetObject(id, OpenMode.ForWrite) as Dimension;
                            Result += acEnt.Measurement;
                        }
                    }
                    acEd.WriteMessage("Total : " + Result.ToString());
                }
                else
                {
                    return;
                }
            }
        }
    }
}
