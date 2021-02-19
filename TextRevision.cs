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

[assembly : CommandClass(typeof(TextRevision.TextRevision)),
    CommandClass(typeof(TextRevision.TextToText)),
    CommandClass(typeof(TextRevision.TextPrefixSuffix)),
    CommandClass(typeof(TextRevision.TextJoin)),
    CommandClass(typeof(TextRevision.TextAlign)),
    CommandClass(typeof(TextRevision.TextAlignWithDistance))]

namespace TextRevision
{
    //Text Replace
    class TextRevision
    {
        [CommandMethod("CT", CommandFlags.Redraw)]
        public static void Ct()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                TypedValue[] acTypValAr = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Operator, "<or"),
                    new TypedValue((int)DxfCode.Start, "TEXT"),
                    new TypedValue((int)DxfCode.Start, "MTEXT"),
                    new TypedValue((int)DxfCode.Operator, "or>")
                 };

                //Current Text
                SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
                PromptSelectionResult acSSPrompt = ed.GetSelection(acSelFtr);
                if (acSSPrompt.Status != PromptStatus.OK)
                    return;

                PromptStringOptions Oldtext = new PromptStringOptions("\nInput Current Text: ");
                Oldtext.AllowSpaces = true;
                PromptResult Oldtextresult = doc.Editor.GetString(Oldtext);
                if (Oldtextresult.Status != PromptStatus.OK)
                    return;
                string Cur_Text = Oldtextresult.StringResult;

                //New Replace Text
                PromptStringOptions Replace = new PromptStringOptions("\nInput Replace Text: ");
                Replace.AllowSpaces = true;
                PromptResult Replaceresult = doc.Editor.GetString(Replace);
                if (Replaceresult.Status != PromptStatus.OK)
                    return;
                string Replace_Text = Replaceresult.StringResult;

                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                    foreach (ObjectId id in ids)
                    {
                        Entity cur_ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                        if (cur_ent == null)
                            return;

                        if (cur_ent.GetType() == typeof(DBText))
                        {
                            DBText txt = trans.GetObject(id, OpenMode.ForRead) as DBText;
                            string txt_content = txt.TextString;
                            txt.UpgradeOpen();
                            txt.TextString = txt_content.Replace(Cur_Text, Replace_Text);
                        }
                        else
                        {
                            MText txt = trans.GetObject(id, OpenMode.ForRead) as MText;
                            if (txt != null)
                            {
                                DBObjectCollection dbs = new DBObjectCollection();
                                txt.Explode(dbs);
                                string s = "";
                                foreach (Entity ent in dbs)
                                {
                                    DBText txtstring = ent as DBText;
                                    if (txtstring != null)
                                    {
                                        s += txtstring.TextString;
                                    }
                                }
                                txt.UpgradeOpen();
                                txt.Contents = s.Replace(Cur_Text, Replace_Text);
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
                trans.Commit();
            }
        }
    }
    class SaveText
    {
        public string Cur_DBText { get; set; }
        public string Cur_MText { get; set; }
    }
    //Text Replace to Text
    class TextToText
    {
        [CommandMethod("CTT", CommandFlags.Redraw)]
        public static void Ctt()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                TypedValue[] acTypValAr = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Operator, "<or"),
                    new TypedValue((int)DxfCode.Start, "TEXT"),
                    new TypedValue((int)DxfCode.Start, "MTEXT"),
                    new TypedValue((int)DxfCode.Operator, "or>")
                 };

                //Current Text
                SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
                PromptSelectionResult acSSPrompt = ed.GetSelection(acSelFtr);
                if (acSSPrompt.Status != PromptStatus.OK)
                    return;

                //Replace Text
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect Replace Text: ");
                peo.SetRejectMessage("\nYou have to select Text or MText...>>");
                peo.AddAllowedClass(typeof(DBText), false);
                peo.AddAllowedClass(typeof(MText), false);
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                    return;

                Entity ent = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                if (ent == null)
                    return;

                SaveText TextInfo = new SaveText();
                if (ent.GetType() == typeof(DBText))
                {
                    DBText Replace_text = ent as DBText;
                    TextInfo.Cur_DBText = Replace_text.TextString;
                }
                else
                {
                    MText txt = ent as MText;
                    if (txt != null)
                    {
                        DBObjectCollection dbs = new DBObjectCollection();
                        txt.Explode(dbs);
                        string s = "";
                        foreach (Entity stringent in dbs)
                        {
                            DBText txtstring = stringent as DBText;
                            if (txtstring != null)
                            {
                                s += txtstring.TextString;
                            }
                        }
                        TextInfo.Cur_MText = s;
                    }
                }


                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                    foreach (ObjectId id in ids)
                    {
                        Entity ent2 = trans.GetObject(id, OpenMode.ForRead) as Entity;

                        if (ent2.GetType() == typeof(DBText))
                        {
                            DBText txt = ent2 as DBText;
                            string txt_content = txt.TextString;
                            if (TextInfo.Cur_DBText != null)
                            {
                                txt.UpgradeOpen();
                                txt.TextString = txt_content.Replace(txt_content, TextInfo.Cur_DBText);
                            }
                            else if (TextInfo.Cur_MText != null)
                            {
                                txt.UpgradeOpen();
                                txt.TextString = txt_content.Replace(txt_content, TextInfo.Cur_MText);
                            }
                        }
                        else
                        {
                            MText txt = ent2 as MText;
                            if (txt != null)
                            {
                                DBObjectCollection dbs = new DBObjectCollection();
                                txt.Explode(dbs);
                                string s = "";
                                foreach (Entity stringent in dbs)
                                {
                                    DBText txtstring = stringent as DBText;
                                    if (txtstring != null)
                                    {
                                        s += txtstring.TextString;
                                    }
                                }
                                if (TextInfo.Cur_DBText != null)
                                {
                                    txt.UpgradeOpen();
                                    txt.Contents = s.Replace(s, TextInfo.Cur_DBText);
                                }
                                else if (TextInfo.Cur_MText != null)
                                {
                                    txt.UpgradeOpen();
                                    txt.Contents = s.Replace(s, TextInfo.Cur_MText);
                                }
                            }
                        }
                    }    
                }
                else
                {
                    return;
                }
                trans.Commit();
            }
        }
    }
    //Text Add Prefix and Suffix
    class TextPrefixSuffix
    {
        [CommandMethod("Tps", CommandFlags.Redraw)]
        public static void Tps()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                //Adding Text Location
                PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
                pKeyOpts.Message = "\nSelect Adding Text Location ";
                pKeyOpts.Keywords.Add("Prefix");
                pKeyOpts.Keywords.Add("Suffix");
                pKeyOpts.AllowNone = true;
                PromptResult pKeyRes = doc.Editor.GetKeywords(pKeyOpts);
                if (pKeyRes.Status != PromptStatus.OK)
                    return;

                TypedValue[] acTypValAr = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Operator, "<or"),
                    new TypedValue((int)DxfCode.Start, "TEXT"),
                    new TypedValue((int)DxfCode.Start, "MTEXT"),
                    new TypedValue((int)DxfCode.Operator, "or>")
                 };

                //Current Text
                SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
                PromptSelectionResult acSSPrompt = ed.GetSelection(acSelFtr);
                if (acSSPrompt.Status != PromptStatus.OK)
                    return;

                //Add Text
                PromptStringOptions Replace = new PromptStringOptions("\nInput Add Text: ");
                Replace.AllowSpaces = true;
                PromptResult Replaceresult = doc.Editor.GetString(Replace);
                if (Replaceresult.Status != PromptStatus.OK)
                    return;
                string Add_Text = Replaceresult.StringResult;

                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                    foreach (ObjectId id in ids)
                    {
                        Entity cur_ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                        if (cur_ent == null)
                            return;

                        if (cur_ent.GetType() == typeof(DBText))
                        {
                            DBText txt = trans.GetObject(id, OpenMode.ForRead) as DBText;
                            string txt_content = txt.TextString;
                            txt.UpgradeOpen();
                            if (pKeyRes.StringResult == "Suffix")
                            {
                                txt.TextString = txt_content + Add_Text;
                            }
                            else
                            {
                                txt.TextString = Add_Text + txt_content;
                            }
                        }
                        else
                        {
                            MText txt = trans.GetObject(id, OpenMode.ForRead) as MText;
                            if (txt != null)
                            {
                                DBObjectCollection dbs = new DBObjectCollection();
                                txt.Explode(dbs);
                                string s = "";
                                foreach (Entity ent in dbs)
                                {
                                    DBText txtstring = ent as DBText;
                                    if (txtstring != null)
                                    {
                                        s += txtstring.TextString;
                                    }
                                }
                                txt.UpgradeOpen();
                                if (pKeyRes.StringResult == "Suffix")
                                {
                                    txt.Contents = s + Add_Text;
                                }
                                else
                                {
                                    txt.Contents = Add_Text + s;
                                }
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
                trans.Commit();
            }
        }
    }
    //Text Join
    class TextJoin
    {
        [CommandMethod("TJ", CommandFlags.Redraw)]
        public static void Textjoin()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //Adding Text Location
            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\nSelect Text Direction ";
            pKeyOpts.Keywords.Add("+X");
            pKeyOpts.Keywords.Add("-X");
            pKeyOpts.Keywords.Add("+Y");
            pKeyOpts.Keywords.Add("-Y");
            pKeyOpts.AllowNone = false;
            PromptResult pKeyRes = doc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK)
                return;

            while (true)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    TypedValue[] acTypValAr = new TypedValue[]
                    {
                    new TypedValue((int)DxfCode.Operator, "<or"),
                    new TypedValue((int)DxfCode.Start, "TEXT"),
                    new TypedValue((int)DxfCode.Start, "MTEXT"),
                    new TypedValue((int)DxfCode.Operator, "or>")
                     };

                    //Current Text
                    SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
                    PromptSelectionResult acSSPrompt = ed.GetSelection(acSelFtr);
                    if (acSSPrompt.Status != PromptStatus.OK)
                        return;

                    if (acSSPrompt.Status == PromptStatus.OK)
                    {
                        if (pKeyRes.StringResult == "+X")
                        {
                            ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                            string s = "";
                            bool check = false; //false is dbtext, true is mtext

                            DBText dbtext = new DBText();
                            MText mtext = new MText();
                            Double X_Value = System.Double.MaxValue;

                            foreach (ObjectId id in ids)
                            {
                                Entity cur_ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                                if (cur_ent == null)
                                    return;

                                if (cur_ent.GetType() == typeof(DBText))
                                {
                                    DBText txt = cur_ent as DBText;
                                    Double Cur_X = txt.Position.X;

                                    if (Cur_X < X_Value)
                                    {
                                        s = txt.TextString + s;
                                        X_Value = Cur_X;
                                        dbtext = txt;
                                    }
                                    else
                                    {
                                        s += txt.TextString;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Erase();
                                }
                                else
                                {
                                    MText txt = cur_ent as MText;
                                    Double Cur_X = txt.Location.X;

                                    if (Cur_X < X_Value)
                                    {
                                        s = txt.Contents + s;
                                        X_Value = Cur_X;
                                        mtext = txt;
                                        check = true;
                                    }
                                    else
                                    {
                                        s += txt.Contents;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Erase();
                                }
                            }
                            if (!check)
                            {
                                using (DBText NewText = new DBText())
                                {
                                    NewText.Position = dbtext.Position;
                                    NewText.TextString = s;
                                    NewText.Rotation = dbtext.Rotation;
                                    NewText.Justify = dbtext.Justify;
                                    NewText.ColorIndex = dbtext.ColorIndex;
                                    NewText.Layer = dbtext.Layer;
                                    btr.AppendEntity(NewText);
                                    trans.AddNewlyCreatedDBObject(NewText, true);
                                }
                            }
                            else
                            {
                                using (MText NewText = new MText())
                                {
                                    NewText.Location = mtext.Location;
                                    NewText.Contents = s;
                                    NewText.Rotation = mtext.Rotation;
                                    NewText.Attachment = mtext.Attachment;
                                    NewText.ColorIndex = mtext.ColorIndex;
                                    NewText.Layer = mtext.Layer;
                                    btr.AppendEntity(NewText);
                                    trans.AddNewlyCreatedDBObject(NewText, true);
                                }
                            }
                        }
                        else if (pKeyRes.StringResult == "-X")
                        {
                            ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                            string s = "";
                            bool check = false; //false is dbtext, true is mtext

                            DBText dbtext = new DBText();
                            MText mtext = new MText();
                            Double X_Value = System.Double.MinValue;

                            foreach (ObjectId id in ids)
                            {
                                Entity cur_ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                                if (cur_ent == null)
                                    return;

                                if (cur_ent.GetType() == typeof(DBText))
                                {
                                    DBText txt = cur_ent as DBText;
                                    Double Cur_X = txt.Position.X;

                                    if (Cur_X > X_Value)
                                    {
                                        s += txt.TextString;
                                        X_Value = Cur_X;
                                        dbtext = txt;
                                    }
                                    else
                                    {
                                        s = txt.TextString + s;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Erase();
                                }
                                else
                                {
                                    MText txt = cur_ent as MText;
                                    Double Cur_X = txt.Location.X;

                                    if (Cur_X > X_Value)
                                    {
                                        s += txt.Contents;
                                        X_Value = Cur_X;
                                        mtext = txt;
                                        check = true;
                                    }
                                    else
                                    {
                                        s = txt.Contents + s;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Erase();
                                }
                            }
                            if (!check)
                            {
                                using (DBText NewText = new DBText())
                                {
                                    NewText.Position = dbtext.Position;
                                    NewText.TextString = s;
                                    NewText.Rotation = dbtext.Rotation;
                                    NewText.Justify = dbtext.Justify;
                                    NewText.ColorIndex = dbtext.ColorIndex;
                                    NewText.Layer = dbtext.Layer;
                                    btr.AppendEntity(NewText);
                                    trans.AddNewlyCreatedDBObject(NewText, true);
                                }
                            }
                            else
                            {
                                using (MText NewText = new MText())
                                {
                                    NewText.Location = mtext.Location;
                                    NewText.Contents = s;
                                    NewText.Rotation = mtext.Rotation;
                                    NewText.Attachment = mtext.Attachment;
                                    NewText.ColorIndex = mtext.ColorIndex;
                                    NewText.Layer = mtext.Layer;
                                    btr.AppendEntity(NewText);
                                    trans.AddNewlyCreatedDBObject(NewText, true);
                                }
                            }
                        }
                        else if (pKeyRes.StringResult == "+Y")
                        {
                            ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                            string s = "";
                            bool check = false; //false is dbtext, true is mtext

                            DBText dbtext = new DBText();
                            MText mtext = new MText();
                            Double Y_Value = System.Double.MaxValue;

                            foreach (ObjectId id in ids)
                            {
                                Entity cur_ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                                if (cur_ent == null)
                                    return;

                                if (cur_ent.GetType() == typeof(DBText))
                                {
                                    DBText txt = cur_ent as DBText;
                                    Double Cur_Y = txt.Position.Y;

                                    if (Cur_Y < Y_Value)
                                    {
                                        s = txt.TextString + s;
                                        Y_Value = Cur_Y;
                                        dbtext = txt;
                                    }
                                    else
                                    {
                                        s += txt.TextString;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Erase();
                                }
                                else
                                {
                                    MText txt = cur_ent as MText;
                                    Double Cur_Y = txt.Location.Y;

                                    if (Cur_Y < Y_Value)
                                    {
                                        s = txt.Contents + s;
                                        Y_Value = Cur_Y;
                                        mtext = txt;
                                        check = true;
                                    }
                                    else
                                    {
                                        s += txt.Contents;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Erase();
                                }
                            }
                            if (!check)
                            {
                                using (DBText NewText = new DBText())
                                {
                                    NewText.Position = dbtext.Position;
                                    NewText.TextString = s;
                                    NewText.Rotation = dbtext.Rotation;
                                    NewText.Justify = dbtext.Justify;
                                    NewText.ColorIndex = dbtext.ColorIndex;
                                    NewText.Layer = dbtext.Layer;
                                    btr.AppendEntity(NewText);
                                    trans.AddNewlyCreatedDBObject(NewText, true);
                                }
                            }
                            else
                            {
                                using (MText NewText = new MText())
                                {
                                    NewText.Location = mtext.Location;
                                    NewText.Contents = s;
                                    NewText.Rotation = mtext.Rotation;
                                    NewText.Attachment = mtext.Attachment;
                                    NewText.ColorIndex = mtext.ColorIndex;
                                    NewText.Layer = mtext.Layer;
                                    btr.AppendEntity(NewText);
                                    trans.AddNewlyCreatedDBObject(NewText, true);
                                }
                            }
                        }
                        else if (pKeyRes.StringResult == "-Y")
                        {
                            ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                            string s = "";
                            bool check = false; //false is dbtext, true is mtext

                            DBText dbtext = new DBText();
                            MText mtext = new MText();
                            Double Y_Value = System.Double.MinValue;

                            foreach (ObjectId id in ids)
                            {
                                Entity cur_ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                                if (cur_ent == null)
                                    return;

                                if (cur_ent.GetType() == typeof(DBText))
                                {
                                    DBText txt = cur_ent as DBText;
                                    Double Cur_Y = txt.Position.Y;

                                    if (Cur_Y > Y_Value)
                                    {
                                        s += txt.TextString;
                                        Y_Value = Cur_Y;
                                        dbtext = txt;
                                    }
                                    else
                                    {
                                        s = txt.TextString + s;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Erase();
                                }
                                else
                                {
                                    MText txt = cur_ent as MText;
                                    Double Cur_Y = txt.Location.Y;

                                    if (Cur_Y > Y_Value)
                                    {
                                        s += txt.Contents;
                                        Y_Value = Cur_Y;
                                        mtext = txt;
                                        check = true;
                                    }
                                    else
                                    {
                                        s = txt.Contents + s;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Erase();
                                }
                            }
                            if (!check)
                            {
                                using (DBText NewText = new DBText())
                                {
                                    NewText.Position = dbtext.Position;
                                    NewText.TextString = s;
                                    NewText.Rotation = dbtext.Rotation;
                                    NewText.Justify = dbtext.Justify;
                                    NewText.ColorIndex = dbtext.ColorIndex;
                                    NewText.Layer = dbtext.Layer;
                                    btr.AppendEntity(NewText);
                                    trans.AddNewlyCreatedDBObject(NewText, true);
                                }
                            }
                            else
                            {
                                using (MText NewText = new MText())
                                {
                                    NewText.Location = mtext.Location;
                                    NewText.Contents = s;
                                    NewText.Rotation = mtext.Rotation;
                                    NewText.Attachment = mtext.Attachment;
                                    NewText.ColorIndex = mtext.ColorIndex;
                                    NewText.Layer = mtext.Layer;
                                    btr.AppendEntity(NewText);
                                    trans.AddNewlyCreatedDBObject(NewText, true);
                                }
                            }
                        }
                        else
                        {
                            ed.WriteMessage("\nKeyword is wrong vlaue.");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                    trans.Commit();
                }
            }
        }
    }
    class LineOrPline
    {
        public Line CurLine { get; set; }
        public Polyline CurPLine { get; set; }
        public double LineAngle { get; set; }
        public double PlineAngle { get; set; }
    }
    class LineInfo
    {
        public Point3dCollection LVerticalXl { get; set; } = new Point3dCollection();
        public Point3dCollection LHorizontalXl { get; set; } = new Point3dCollection();
        public Point3dCollection PlVerticalXl { get; set; } = new Point3dCollection();
        public Point3dCollection PlHorizontalXl { get; set; } = new Point3dCollection();
        public Point3d IntersectionPoint { get; set; }
        public double txtXval { get; set; }
        public double txtYval { get; set; }
    }
    //Text Align
    class TextAlign
    {
        [CommandMethod("TA", CommandFlags.Redraw)]
        public static void Textalign()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\nSelect Text Align Direction >> ";
            pKeyOpts.Keywords.Add("Vertical");
            pKeyOpts.Keywords.Add("Horizontal");
            pKeyOpts.AllowNone = false;
            PromptResult pKeyRes = doc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK)
                return;

            while (true)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    //Text Select
                    TypedValue[] acTypValAr = new TypedValue[]
                    {
                         };
                    SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
                    PromptSelectionResult acSSPrompt = ed.GetSelection(acSelFtr);
                    if (acSSPrompt.Status != PromptStatus.OK)
                        return;

                    //Select Line or Polyline
                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect second line: ");
                    peo.SetRejectMessage("\nYou have to select polyline or line...>>");
                    peo.AddAllowedClass(typeof(Polyline), false);
                    peo.AddAllowedClass(typeof(Line), false);
                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;

                    Entity cur_line = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (cur_line == null)
                        return;

                    LineOrPline lop = new LineOrPline();

                    if (cur_line.GetType() == typeof(Line))
                    {
                        lop.CurLine = cur_line as Line;
                        lop.LineAngle = lop.CurLine.Angle;
                    }
                    else
                    {
                        lop.CurPLine = cur_line as Polyline;
                    }

                    //Main
                    if (acSSPrompt.Status == PromptStatus.OK)
                    {
                        ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                        foreach (ObjectId id in ids)
                        {
                            Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null)
                                return;

                            LineInfo CurInfo = new LineInfo();

                            //DBText
                            if (ent.GetType() == typeof(DBText))
                            {
                                DBText txt = ent as DBText;
                                CurInfo.txtXval = txt.Position.X;
                                CurInfo.txtYval = txt.Position.Y;

                                using (Xline Verticalxl = new Xline())
                                {
                                    Verticalxl.BasePoint = new Point3d(CurInfo.txtXval, 0, 0);
                                    Verticalxl.SecondPoint = new Point3d(CurInfo.txtXval, 1, 0);

                                    if (lop.CurLine != null)
                                    {
                                        lop.CurLine.IntersectWith(Verticalxl, Intersect.OnBothOperands, CurInfo.LVerticalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        lop.CurPLine.IntersectWith(Verticalxl, Intersect.OnBothOperands, CurInfo.PlVerticalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                }

                                using (Xline Horizontalxl = new Xline())
                                {
                                    Horizontalxl.BasePoint = new Point3d(0, CurInfo.txtYval, 0);
                                    Horizontalxl.SecondPoint = new Point3d(1, CurInfo.txtYval, 0);

                                    if (lop.CurLine != null)
                                    {
                                        lop.CurLine.IntersectWith(Horizontalxl, Intersect.OnBothOperands, CurInfo.LHorizontalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        lop.CurPLine.IntersectWith(Horizontalxl, Intersect.OnBothOperands, CurInfo.PlHorizontalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                }

                                double dist = System.Double.MaxValue;

                                if (CurInfo.LVerticalXl.Count > 0) //Text Horizontal
                                {
                                    foreach (Point3d pt in CurInfo.LVerticalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtYval - pt.Y) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtYval - pt.Y);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    txt.UpgradeOpen();

                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (3 * Math.PI / 2 >= lop.LineAngle & lop.LineAngle >= Math.PI / 2)
                                        {
                                            txt.Rotation = lop.LineAngle - (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle + (Math.PI / 2);
                                        }
                                        txt.Position = new Point3d(txt.Position.X, CurInfo.IntersectionPoint.Y, txt.Position.Z);
                                    }
                                    else
                                    {
                                        if (3 * Math.PI / 2 >= lop.LineAngle & lop.LineAngle >= Math.PI / 2)
                                        {
                                            txt.Rotation = lop.LineAngle - Math.PI;
                                            txt.Position = new Point3d(txt.Position.X, CurInfo.IntersectionPoint.Y, txt.Position.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle;
                                            txt.Position = new Point3d(txt.Position.X, CurInfo.IntersectionPoint.Y, txt.Position.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.LHorizontalXl.Count > 0) //Text Vertical
                                {
                                    foreach (Point3d pt in CurInfo.LHorizontalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtXval - pt.X) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtXval - pt.X);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    txt.UpgradeOpen();

                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.LineAngle >= Math.PI)
                                        {
                                            txt.Rotation = lop.LineAngle - (3 * Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle - (Math.PI / 2);
                                        }
                                        txt.Position = new Point3d(CurInfo.IntersectionPoint.X, txt.Position.Y, txt.Position.Z);
                                    }
                                    else
                                    {
                                        if (lop.LineAngle >= Math.PI)
                                        {
                                            txt.Rotation = lop.LineAngle - Math.PI;
                                            txt.Position = new Point3d(CurInfo.IntersectionPoint.X, txt.Position.Y, txt.Position.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle;
                                            txt.Position = new Point3d(CurInfo.IntersectionPoint.X, txt.Position.Y, txt.Position.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.PlVerticalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.PlVerticalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtYval - pt.Y) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtYval - pt.Y);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    Point3d point = lop.CurPLine.GetClosestPointTo(CurInfo.IntersectionPoint, false);
                                    double parameter = lop.CurPLine.GetParameterAtPoint(point);
                                    int index = (int)parameter;

                                    if (lop.CurPLine.GetSegmentType(index) == SegmentType.Line)
                                    {
                                        LineSegment2d segment = lop.CurPLine.GetLineSegment2dAt(index);
                                        lop.PlineAngle = segment.Direction.Angle;
                                    }
                                    txt.UpgradeOpen();

                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.PlineAngle <= Math.PI / 2 || lop.PlineAngle >= 3 * Math.PI / 2)
                                        {
                                            txt.Rotation = lop.PlineAngle + (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - (Math.PI / 2);
                                        }
                                        txt.Position = new Point3d(txt.Position.X, CurInfo.IntersectionPoint.Y, txt.Position.Z);
                                    }
                                    else
                                    {
                                        if (lop.PlineAngle <= Math.PI / 2 || lop.PlineAngle >= 3 * Math.PI / 2)
                                        {
                                            txt.Rotation = lop.PlineAngle;
                                            txt.Position = new Point3d(txt.Position.X, CurInfo.IntersectionPoint.Y, txt.Position.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - Math.PI;
                                            txt.Position = new Point3d(txt.Position.X, CurInfo.IntersectionPoint.Y, txt.Position.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.PlHorizontalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.PlHorizontalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtXval - pt.X) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtXval - pt.X);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    Point3d point = lop.CurPLine.GetClosestPointTo(CurInfo.IntersectionPoint, false);
                                    double parameter = lop.CurPLine.GetParameterAtPoint(point);
                                    int index = (int)parameter;

                                    if (lop.CurPLine.GetSegmentType(index) == SegmentType.Line)
                                    {
                                        LineSegment2d segment = lop.CurPLine.GetLineSegment2dAt(index);
                                        lop.PlineAngle = segment.Direction.Angle;
                                    }
                                    txt.UpgradeOpen();

                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.PlineAngle <= Math.PI)
                                        {
                                            txt.Rotation = lop.PlineAngle - (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle + (Math.PI / 2);
                                        }
                                        txt.Position = new Point3d(CurInfo.IntersectionPoint.X, txt.Position.Y, txt.Position.Z);
                                    }
                                    else
                                    {
                                        if (lop.PlineAngle <= Math.PI)
                                        {
                                            txt.Rotation = lop.PlineAngle;
                                            txt.Position = new Point3d(CurInfo.IntersectionPoint.X, txt.Position.Y, txt.Position.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - Math.PI;
                                            txt.Position = new Point3d(CurInfo.IntersectionPoint.X, txt.Position.Y, txt.Position.Z);
                                        }
                                    }
                                }
                            }
                            //MText
                            else if (ent.GetType() == typeof(MText))
                            {
                                MText txt = ent as MText;
                                CurInfo.txtXval = txt.Location.X;
                                CurInfo.txtYval = txt.Location.Y;

                                using (Xline Verticalxl = new Xline())
                                {
                                    Verticalxl.BasePoint = new Point3d(CurInfo.txtXval, 0, 0);
                                    Verticalxl.SecondPoint = new Point3d(CurInfo.txtXval, 1, 0);

                                    if (lop.CurLine != null)
                                    {
                                        lop.CurLine.IntersectWith(Verticalxl, Intersect.OnBothOperands, CurInfo.LVerticalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        lop.CurPLine.IntersectWith(Verticalxl, Intersect.OnBothOperands, CurInfo.PlVerticalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                }

                                using (Xline Horizontalxl = new Xline())
                                {
                                    Horizontalxl.BasePoint = new Point3d(0, CurInfo.txtYval, 0);
                                    Horizontalxl.SecondPoint = new Point3d(1, CurInfo.txtYval, 0);

                                    if (lop.CurLine != null)
                                    {
                                        lop.CurLine.IntersectWith(Horizontalxl, Intersect.OnBothOperands, CurInfo.LHorizontalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        lop.CurPLine.IntersectWith(Horizontalxl, Intersect.OnBothOperands, CurInfo.PlHorizontalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                }

                                double dist = System.Double.MaxValue;

                                if (CurInfo.LVerticalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.LVerticalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtYval - pt.Y) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtYval - pt.Y);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    txt.UpgradeOpen();
                                    txt.Attachment = AttachmentPoint.BottomLeft;
                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (3 * Math.PI / 2 >= lop.LineAngle & lop.LineAngle >= Math.PI / 2)
                                        {
                                            txt.Rotation = lop.LineAngle - (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle + (Math.PI / 2);
                                        }
                                        txt.Location = new Point3d(txt.Location.X, CurInfo.IntersectionPoint.Y, txt.Location.Z);
                                    }
                                    else
                                    {
                                        if (3 * Math.PI / 2 >= lop.LineAngle & lop.LineAngle >= Math.PI / 2)
                                        {
                                            txt.Rotation = lop.LineAngle - Math.PI;
                                            txt.Location = new Point3d(txt.Location.X, CurInfo.IntersectionPoint.Y, txt.Location.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle;
                                            txt.Location = new Point3d(txt.Location.X, CurInfo.IntersectionPoint.Y, txt.Location.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.LHorizontalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.LHorizontalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtXval - pt.X) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtXval - pt.X);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    txt.UpgradeOpen();
                                    txt.Attachment = AttachmentPoint.BottomLeft;
                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.LineAngle >= Math.PI)
                                        {
                                            txt.Rotation = lop.LineAngle - (3 * Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle - (Math.PI / 2);
                                        }
                                        txt.Location = new Point3d(CurInfo.IntersectionPoint.X, txt.Location.Y, txt.Location.Z);
                                    }
                                    else
                                    {
                                        if (lop.LineAngle >= Math.PI)
                                        {
                                            txt.Rotation = lop.LineAngle - Math.PI;
                                            txt.Location = new Point3d(CurInfo.IntersectionPoint.X, txt.Location.Y, txt.Location.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle;
                                            txt.Location = new Point3d(CurInfo.IntersectionPoint.X, txt.Location.Y, txt.Location.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.PlVerticalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.PlVerticalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtYval - pt.Y) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtYval - pt.Y);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    Point3d point = lop.CurPLine.GetClosestPointTo(CurInfo.IntersectionPoint, false);
                                    double parameter = lop.CurPLine.GetParameterAtPoint(point);
                                    int index = (int)parameter;

                                    if (lop.CurPLine.GetSegmentType(index) == SegmentType.Line)
                                    {
                                        LineSegment2d segment = lop.CurPLine.GetLineSegment2dAt(index);
                                        lop.PlineAngle = segment.Direction.Angle;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Attachment = AttachmentPoint.BottomLeft;
                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.PlineAngle <= Math.PI / 2 || lop.PlineAngle >= 3 * Math.PI / 2)
                                        {
                                            txt.Rotation = lop.PlineAngle + (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - (Math.PI / 2);
                                        }
                                        txt.Location = new Point3d(txt.Location.X, CurInfo.IntersectionPoint.Y, txt.Location.Z);
                                    }
                                    else
                                    {
                                        if (lop.PlineAngle <= Math.PI / 2 || lop.PlineAngle >= 3 * Math.PI / 2)
                                        {
                                            txt.Rotation = lop.PlineAngle;
                                            txt.Location = new Point3d(txt.Location.X, CurInfo.IntersectionPoint.Y, txt.Location.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - Math.PI;
                                            txt.Location = new Point3d(txt.Location.X, CurInfo.IntersectionPoint.Y, txt.Location.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.PlHorizontalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.PlHorizontalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtXval - pt.X) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtXval - pt.X);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    Point3d point = lop.CurPLine.GetClosestPointTo(CurInfo.IntersectionPoint, false);
                                    double parameter = lop.CurPLine.GetParameterAtPoint(point);
                                    int index = (int)parameter;

                                    if (lop.CurPLine.GetSegmentType(index) == SegmentType.Line)
                                    {
                                        LineSegment2d segment = lop.CurPLine.GetLineSegment2dAt(index);
                                        lop.PlineAngle = segment.Direction.Angle;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Attachment = AttachmentPoint.BottomLeft;
                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.PlineAngle <= Math.PI)
                                        {
                                            txt.Rotation = lop.PlineAngle - (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle + (Math.PI / 2);
                                        }
                                        txt.Location = new Point3d(CurInfo.IntersectionPoint.X, txt.Location.Y, txt.Location.Z);
                                    }
                                    else
                                    {
                                        if (lop.PlineAngle <= Math.PI)
                                        {
                                            txt.Rotation = lop.PlineAngle;
                                            txt.Location = new Point3d(CurInfo.IntersectionPoint.X, txt.Location.Y, txt.Location.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - Math.PI;
                                            txt.Location = new Point3d(CurInfo.IntersectionPoint.X, txt.Location.Y, txt.Location.Z);
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
    class TextAlignWithDistance
    {
        [CommandMethod("TAA", CommandFlags.Redraw)]
        public static void TextalignWithDistance()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\nSelect Text Align Direction >> ";
            pKeyOpts.Keywords.Add("Vertical");
            pKeyOpts.Keywords.Add("Horizontal");
            pKeyOpts.AllowNone = false;
            PromptResult pKeyRes = doc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.Status != PromptStatus.OK)
                return;

            while (true)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    //Get Distance
                    PromptStringOptions pso = new PromptStringOptions("\nEnter Offset Distance: ");
                    pso.AllowSpaces = false;
                    PromptResult strresult = doc.Editor.GetString(pso);
                    if (strresult.Status != PromptStatus.OK)
                        return;

                    int distance = Convert.ToInt32(strresult.StringResult);

                    //Text Select
                    TypedValue[] acTypValAr = new TypedValue[]
                    {
                new TypedValue((int)DxfCode.Operator, "<or"),
                new TypedValue((int)DxfCode.Start, "TEXT"),
                new TypedValue((int)DxfCode.Start, "MTEXT"),
                new TypedValue((int)DxfCode.Operator, "or>")
                        };
                    SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);
                    PromptSelectionResult acSSPrompt = ed.GetSelection(acSelFtr);
                    if (acSSPrompt.Status != PromptStatus.OK)
                        return;

                    //Select Line or Polyline
                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect second line: ");
                    peo.SetRejectMessage("\nYou have to select polyline or line...>>");
                    peo.AddAllowedClass(typeof(Polyline), false);
                    peo.AddAllowedClass(typeof(Line), false);
                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;

                    Entity cur_line = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (cur_line == null)
                        return;

                    LineOrPline lop = new LineOrPline();

                    if (cur_line.GetType() == typeof(Line))
                    {
                        lop.CurLine = cur_line as Line;
                        lop.LineAngle = lop.CurLine.Angle;
                    }
                    else
                    {
                        lop.CurPLine = cur_line as Polyline;
                    }

                    //Main
                    if (acSSPrompt.Status == PromptStatus.OK)
                    {
                        ObjectId[] ids = acSSPrompt.Value.GetObjectIds();

                        foreach (ObjectId id in ids)
                        {
                            Entity ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null)
                                return;

                            LineInfo CurInfo = new LineInfo();

                            //DBText
                            if (ent.GetType() == typeof(DBText))
                            {
                                DBText txt = ent as DBText;
                                CurInfo.txtXval = txt.Position.X;
                                CurInfo.txtYval = txt.Position.Y;

                                using (Xline Verticalxl = new Xline())
                                {
                                    Verticalxl.BasePoint = new Point3d(CurInfo.txtXval, 0, 0);
                                    Verticalxl.SecondPoint = new Point3d(CurInfo.txtXval, 1, 0);

                                    if (lop.CurLine != null)
                                    {
                                        lop.CurLine.IntersectWith(Verticalxl, Intersect.OnBothOperands, CurInfo.LVerticalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        lop.CurPLine.IntersectWith(Verticalxl, Intersect.OnBothOperands, CurInfo.PlVerticalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                }

                                using (Xline Horizontalxl = new Xline())
                                {
                                    Horizontalxl.BasePoint = new Point3d(0, CurInfo.txtYval, 0);
                                    Horizontalxl.SecondPoint = new Point3d(1, CurInfo.txtYval, 0);

                                    if (lop.CurLine != null)
                                    {
                                        lop.CurLine.IntersectWith(Horizontalxl, Intersect.OnBothOperands, CurInfo.LHorizontalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        lop.CurPLine.IntersectWith(Horizontalxl, Intersect.OnBothOperands, CurInfo.PlHorizontalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                }

                                double dist = System.Double.MaxValue;

                                if (CurInfo.LVerticalXl.Count > 0) //Text Horizontal
                                {
                                    foreach (Point3d pt in CurInfo.LVerticalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtYval - pt.Y) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtYval - pt.Y);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    txt.UpgradeOpen();

                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (3 * Math.PI / 2 >= lop.LineAngle & lop.LineAngle >= Math.PI / 2)
                                        {
                                            txt.Rotation = lop.LineAngle - (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle + (Math.PI / 2);
                                        }
                                        txt.Position = new Point3d(txt.Position.X + distance * Math.Cos(txt.Rotation), CurInfo.IntersectionPoint.Y + distance * Math.Sin(txt.Rotation), txt.Position.Z);
                                    }
                                    else
                                    {
                                        if (3 * Math.PI / 2 >= lop.LineAngle & lop.LineAngle >= Math.PI / 2)
                                        {
                                            txt.Rotation = lop.LineAngle - Math.PI;
                                            txt.Position = new Point3d(txt.Position.X + distance * Math.Cos(lop.LineAngle - (Math.PI / 2)), CurInfo.IntersectionPoint.Y + distance * Math.Sin(lop.LineAngle - (Math.PI / 2)), txt.Position.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle;
                                            txt.Position = new Point3d(txt.Position.X + distance * Math.Cos(txt.Rotation + (Math.PI / 2)), CurInfo.IntersectionPoint.Y + distance * Math.Sin(txt.Rotation + (Math.PI / 2)), txt.Position.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.LHorizontalXl.Count > 0) //Text Vertical
                                {
                                    foreach (Point3d pt in CurInfo.LHorizontalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtXval - pt.X) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtXval - pt.X);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    txt.UpgradeOpen();

                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.LineAngle >= Math.PI)
                                        {
                                            txt.Rotation = lop.LineAngle - (3 * Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle - (Math.PI / 2);
                                        }
                                        txt.Position = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(txt.Rotation), txt.Position.Y + distance * Math.Sin(txt.Rotation), txt.Position.Z);
                                    }
                                    else
                                    {
                                        if (lop.LineAngle >= Math.PI)
                                        {
                                            txt.Rotation = lop.LineAngle - Math.PI;
                                            txt.Position = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(lop.LineAngle + (3 * Math.PI / 2)), txt.Position.Y + distance * Math.Sin(lop.LineAngle + (3 * Math.PI / 2)), txt.Position.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle;
                                            txt.Position = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(txt.Rotation + (Math.PI / 2)), txt.Position.Y + distance * Math.Sin(txt.Rotation + (Math.PI / 2)), txt.Position.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.PlVerticalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.PlVerticalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtYval - pt.Y) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtYval - pt.Y);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    Point3d point = lop.CurPLine.GetClosestPointTo(CurInfo.IntersectionPoint, false);
                                    double parameter = lop.CurPLine.GetParameterAtPoint(point);
                                    int index = (int)parameter;

                                    if (lop.CurPLine.GetSegmentType(index) == SegmentType.Line)
                                    {
                                        LineSegment2d segment = lop.CurPLine.GetLineSegment2dAt(index);
                                        lop.PlineAngle = segment.Direction.Angle;
                                    }
                                    txt.UpgradeOpen();

                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.PlineAngle <= Math.PI / 2 || lop.PlineAngle >= 3 * Math.PI / 2)
                                        {
                                            txt.Rotation = lop.PlineAngle + (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - (Math.PI / 2);
                                        }
                                        txt.Position = new Point3d(txt.Position.X + distance * Math.Cos(txt.Rotation), CurInfo.IntersectionPoint.Y + distance * Math.Sin(txt.Rotation), txt.Position.Z);
                                    }
                                    else
                                    {
                                        if (lop.PlineAngle <= Math.PI / 2 || lop.PlineAngle >= 3 * Math.PI / 2)
                                        {
                                            txt.Rotation = lop.PlineAngle;
                                            txt.Position = new Point3d(txt.Position.X + distance * Math.Cos(txt.Rotation + (Math.PI / 2)), CurInfo.IntersectionPoint.Y + distance * Math.Sin(txt.Rotation + (Math.PI / 2)), txt.Position.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - Math.PI;
                                            txt.Position = new Point3d(txt.Position.X + distance * Math.Cos(lop.PlineAngle - (Math.PI / 2)), CurInfo.IntersectionPoint.Y + distance * Math.Sin(lop.PlineAngle - (Math.PI / 2)), txt.Position.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.PlHorizontalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.PlHorizontalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtXval - pt.X) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtXval - pt.X);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    Point3d point = lop.CurPLine.GetClosestPointTo(CurInfo.IntersectionPoint, false);
                                    double parameter = lop.CurPLine.GetParameterAtPoint(point);
                                    int index = (int)parameter;

                                    if (lop.CurPLine.GetSegmentType(index) == SegmentType.Line)
                                    {
                                        LineSegment2d segment = lop.CurPLine.GetLineSegment2dAt(index);
                                        lop.PlineAngle = segment.Direction.Angle;
                                    }
                                    txt.UpgradeOpen();

                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.PlineAngle <= Math.PI)
                                        {
                                            txt.Rotation = lop.PlineAngle - (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle + (Math.PI / 2);
                                        }
                                        txt.Position = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(txt.Rotation), txt.Position.Y + distance * Math.Sin(txt.Rotation), txt.Position.Z);
                                    }
                                    else
                                    {
                                        if (lop.PlineAngle <= Math.PI)
                                        {
                                            txt.Rotation = lop.PlineAngle;
                                            txt.Position = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(txt.Rotation + (Math.PI / 2)), txt.Position.Y + distance * Math.Sin(txt.Rotation + (Math.PI / 2)), txt.Position.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - Math.PI;
                                            txt.Position = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(lop.PlineAngle - (Math.PI / 2)), txt.Position.Y + distance * Math.Sin(lop.PlineAngle - (Math.PI / 2)), txt.Position.Z);
                                        }
                                    }
                                }
                            }
                            //MText
                            else if (ent.GetType() == typeof(MText))
                            {
                                MText txt = ent as MText;
                                CurInfo.txtXval = txt.Location.X;
                                CurInfo.txtYval = txt.Location.Y;

                                using (Xline Verticalxl = new Xline())
                                {
                                    Verticalxl.BasePoint = new Point3d(CurInfo.txtXval, 0, 0);
                                    Verticalxl.SecondPoint = new Point3d(CurInfo.txtXval, 1, 0);

                                    if (lop.CurLine != null)
                                    {
                                        lop.CurLine.IntersectWith(Verticalxl, Intersect.OnBothOperands, CurInfo.LVerticalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        lop.CurPLine.IntersectWith(Verticalxl, Intersect.OnBothOperands, CurInfo.PlVerticalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                }

                                using (Xline Horizontalxl = new Xline())
                                {
                                    Horizontalxl.BasePoint = new Point3d(0, CurInfo.txtYval, 0);
                                    Horizontalxl.SecondPoint = new Point3d(1, CurInfo.txtYval, 0);

                                    if (lop.CurLine != null)
                                    {
                                        lop.CurLine.IntersectWith(Horizontalxl, Intersect.OnBothOperands, CurInfo.LHorizontalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        lop.CurPLine.IntersectWith(Horizontalxl, Intersect.OnBothOperands, CurInfo.PlHorizontalXl, IntPtr.Zero, IntPtr.Zero);
                                    }
                                }

                                double dist = System.Double.MaxValue;

                                if (CurInfo.LVerticalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.LVerticalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtYval - pt.Y) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtYval - pt.Y);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    txt.UpgradeOpen();
                                    txt.Attachment = AttachmentPoint.BottomLeft;
                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (3 * Math.PI / 2 >= lop.LineAngle & lop.LineAngle >= Math.PI / 2)
                                        {
                                            txt.Rotation = lop.LineAngle - (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle + (Math.PI / 2);
                                        }
                                        txt.Location = new Point3d(txt.Location.X + distance * Math.Cos(txt.Rotation), CurInfo.IntersectionPoint.Y + distance * Math.Sin(txt.Rotation), txt.Location.Z);
                                    }
                                    else
                                    {
                                        if (3 * Math.PI / 2 >= lop.LineAngle & lop.LineAngle >= Math.PI / 2)
                                        {
                                            txt.Rotation = lop.LineAngle - Math.PI;
                                            txt.Location = new Point3d(txt.Location.X + distance * Math.Cos(lop.LineAngle - (Math.PI / 2)), CurInfo.IntersectionPoint.Y + distance * Math.Sin(lop.LineAngle - (Math.PI / 2)), txt.Location.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle;
                                            txt.Location = new Point3d(txt.Location.X + distance * Math.Cos(txt.Rotation + (Math.PI / 2)), CurInfo.IntersectionPoint.Y + distance * Math.Sin(txt.Rotation + (Math.PI / 2)), txt.Location.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.LHorizontalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.LHorizontalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtXval - pt.X) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtXval - pt.X);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    txt.UpgradeOpen();
                                    txt.Attachment = AttachmentPoint.BottomLeft;
                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.LineAngle >= Math.PI)
                                        {
                                            txt.Rotation = lop.LineAngle - (3 * Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle - (Math.PI / 2);
                                        }
                                        txt.Location = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(txt.Rotation), txt.Location.Y + distance * Math.Sin(txt.Rotation), txt.Location.Z);
                                    }
                                    else
                                    {
                                        if (lop.LineAngle >= Math.PI)
                                        {
                                            txt.Rotation = lop.LineAngle - Math.PI;
                                            txt.Location = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(lop.LineAngle + (3 * Math.PI / 2)), txt.Location.Y + distance * Math.Sin(lop.LineAngle + (3 * Math.PI / 2)), txt.Location.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.LineAngle;
                                            txt.Location = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(txt.Rotation + (Math.PI / 2)), txt.Location.Y + distance * Math.Sin(txt.Rotation + (Math.PI / 2)), txt.Location.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.PlVerticalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.PlVerticalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtYval - pt.Y) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtYval - pt.Y);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    Point3d point = lop.CurPLine.GetClosestPointTo(CurInfo.IntersectionPoint, false);
                                    double parameter = lop.CurPLine.GetParameterAtPoint(point);
                                    int index = (int)parameter;

                                    if (lop.CurPLine.GetSegmentType(index) == SegmentType.Line)
                                    {
                                        LineSegment2d segment = lop.CurPLine.GetLineSegment2dAt(index);
                                        lop.PlineAngle = segment.Direction.Angle;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Attachment = AttachmentPoint.BottomLeft;
                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.PlineAngle <= Math.PI / 2 || lop.PlineAngle >= 3 * Math.PI / 2)
                                        {
                                            txt.Rotation = lop.PlineAngle + (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - (Math.PI / 2);
                                        }
                                        txt.Location = new Point3d(txt.Location.X + distance * Math.Cos(txt.Rotation), CurInfo.IntersectionPoint.Y + distance * Math.Sin(txt.Rotation), txt.Location.Z);
                                    }
                                    else
                                    {
                                        if (lop.PlineAngle <= Math.PI / 2 || lop.PlineAngle >= 3 * Math.PI / 2)
                                        {
                                            txt.Rotation = lop.PlineAngle;
                                            txt.Location = new Point3d(txt.Location.X + distance * Math.Cos(txt.Rotation + (Math.PI / 2)), CurInfo.IntersectionPoint.Y + distance * Math.Sin(txt.Rotation + (Math.PI / 2)), txt.Location.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - Math.PI;
                                            txt.Location = new Point3d(txt.Location.X + distance * Math.Cos(lop.PlineAngle - (Math.PI / 2)), CurInfo.IntersectionPoint.Y + distance * Math.Sin(lop.PlineAngle - (Math.PI / 2)), txt.Location.Z);
                                        }
                                    }
                                }
                                else if (CurInfo.PlHorizontalXl.Count > 0)
                                {
                                    foreach (Point3d pt in CurInfo.PlHorizontalXl)
                                    {
                                        if (Math.Abs(CurInfo.txtXval - pt.X) < dist)
                                        {
                                            dist = Math.Abs(CurInfo.txtXval - pt.X);
                                            CurInfo.IntersectionPoint = pt;
                                        }
                                    }
                                    Point3d point = lop.CurPLine.GetClosestPointTo(CurInfo.IntersectionPoint, false);
                                    double parameter = lop.CurPLine.GetParameterAtPoint(point);
                                    int index = (int)parameter;

                                    if (lop.CurPLine.GetSegmentType(index) == SegmentType.Line)
                                    {
                                        LineSegment2d segment = lop.CurPLine.GetLineSegment2dAt(index);
                                        lop.PlineAngle = segment.Direction.Angle;
                                    }
                                    txt.UpgradeOpen();
                                    txt.Attachment = AttachmentPoint.BottomLeft;
                                    if (pKeyRes.StringResult == "Vertical")
                                    {
                                        if (lop.PlineAngle <= Math.PI)
                                        {
                                            txt.Rotation = lop.PlineAngle - (Math.PI / 2);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle + (Math.PI / 2);
                                        }
                                        txt.Location = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(txt.Rotation), txt.Location.Y + distance * Math.Sin(txt.Rotation), txt.Location.Z);
                                    }
                                    else
                                    {
                                        if (lop.PlineAngle <= Math.PI)
                                        {
                                            txt.Rotation = lop.PlineAngle;
                                            txt.Location = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(txt.Rotation + (Math.PI / 2)), txt.Location.Y + distance * Math.Sin(txt.Rotation + (Math.PI / 2)), txt.Location.Z);
                                        }
                                        else
                                        {
                                            txt.Rotation = lop.PlineAngle - Math.PI;
                                            txt.Location = new Point3d(CurInfo.IntersectionPoint.X + distance * Math.Cos(lop.PlineAngle - (Math.PI / 2)), txt.Location.Y + distance * Math.Sin(lop.PlineAngle - (Math.PI / 2)), txt.Location.Z);
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
}