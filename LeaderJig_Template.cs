using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace LeaderJig_Template
{
    public class LeaderJig_Template
    {
        class DirectionalLeaderJig : EntityJig
        {
            private Point3d _start, _end;
            private string _contents;
            private int _index;
            private int _lineIndex;
            private bool _started;

            public DirectionalLeaderJig(string txt, Point3d start, MLeader ld) : base(ld)
            {
                _contents = txt;
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
                        ml.ContentType = ContentType.MTextContent;

                        var mt = new MText();
                        mt.Contents = _contents;

                        ml.MText = mt;

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

        [CommandMethod("DL")]
        public void DirectionalLeader()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var pso = new PromptStringOptions("\nEnter text");
            pso.AllowSpaces = true;
            var pr = ed.GetString(pso);

            if (pr.Status != PromptStatus.OK)
                return;

            var ppr = ed.GetPoint("\nStart point of leader");
            if (ppr.Status != PromptStatus.OK)
                return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false);

                var ml = new MLeader();
                ml.Visible = false;

                var jig = new DirectionalLeaderJig(pr.StringResult, ppr.Value, ml);

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
}