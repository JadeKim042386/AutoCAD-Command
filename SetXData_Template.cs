using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace SetXData_Template
{
    class SetXData_Template
    {
		static public void SetXData()
		{
			Document doc = Application.DocumentManager.MdiActiveDocument;

			Editor ed = doc.Editor;

			// Select Entity

			PromptEntityOptions opt = new PromptEntityOptions("\nSelect entity: ");

			PromptEntityResult res = ed.GetEntity(opt);


			if (res.Status == PromptStatus.OK)
			{
				using (Transaction tr = doc.TransactionManager.StartTransaction())
				{
					DBObject obj = tr.GetObject(res.ObjectId, OpenMode.ForWrite);

					AddRegAppTableRecord("Jade");

					ResultBuffer rb = new ResultBuffer(new TypedValue(1001, "Jade"), new TypedValue(1000, "This is a test string"));

					obj.XData = rb;

					rb.Dispose();

					tr.Commit();
				}
			}
		}

		static public void AddRegAppTableRecord(string regAppName)
		{
			Document doc = Application.DocumentManager.MdiActiveDocument;

			Database db = doc.Database;

			using (Transaction tr = doc.TransactionManager.StartTransaction())
			{
				RegAppTable rat = tr.GetObject(db.RegAppTableId, OpenMode.ForRead, false) as RegAppTable;

				if (!rat.Has(regAppName))
				{
					rat.UpgradeOpen();

					RegAppTableRecord ratr = new RegAppTableRecord();

					ratr.Name = regAppName;

					rat.Add(ratr);

					tr.AddNewlyCreatedDBObject(ratr, true);
				}
				tr.Commit();
			}
		}
	}
}