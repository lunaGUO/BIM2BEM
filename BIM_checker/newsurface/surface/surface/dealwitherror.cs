using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;

namespace surface
{
    class dealwitherror
    {
        public class MyFailuresPreprocessor : IFailuresPreprocessor
        {
            private string _failureMessage;
            private bool _hasError;
            public string FailureMessage
            {
                get { return _failureMessage; }
                set { _failureMessage = value; }
            }
            public bool HasError
            {
                get { return _hasError; }
                set { _hasError = value; }
            }
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                //获取所有的失败信息
                IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();
                if (failures.Count == 0)
                    return FailureProcessingResult.Continue;

                foreach (FailureMessageAccessor failure in failures)
                {
                    //如果是错误弹出弹框
                    //if (failure.GetSeverity() == FailureSeverity.Error)
                    //{
                        //_failureMessage = failure.GetDescriptionText(); // get the failure description                
                        //_hasError = true;
                        //TaskDialog.Show("错误警告", "FailureProcessingResult.ProceedWithRollBack");
                        //return FailureProcessingResult.ProceedWithRollBack;
                    //}
                    //如果是警告，则禁止弹框
                    if (failure.GetSeverity() == FailureSeverity.Warning)
                    {
                        failuresAccessor.DeleteWarning(failure);
                    }
                }
                return FailureProcessingResult.Continue;
            }
        }
    }
}
