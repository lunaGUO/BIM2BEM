using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace columns
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

            /*
            FailuresAccessor.GetFailureMessages() 获取所有的失败信息
            FailureMessageAccessor.GetSeverity() 可以得知它是警告还是错误
            FailureMessageAccessor.GetDescriptionText() 可以获取错误的文字
            FailureMessageAccessor.GetFailureDefinitionId() 获取失败的定义
            FailuresAccessor.DeleteWarning删除警告，或者FailuresAccessor.DeleteAllWarnings直接删除所有警告
             */

            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                //获取所有的失败信息
                IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();
                if (failures.Count == 0)
                    return FailureProcessingResult.Continue;

                foreach (FailureMessageAccessor failure in failures)
                {
                    //如果是错误则尝试解决
                    if (failure.GetSeverity() == FailureSeverity.Error)
                    {
                        _failureMessage = failure.GetDescriptionText(); // get the failure description                
                        _hasError = true;
                        //if (_failureMessage.Contains("无法使图元保持连接"))
                        //{
                        //TaskDialog.Show("error1", _failureMessage);
                        failuresAccessor.ResolveFailure(failure);
                        return FailureProcessingResult.ProceedWithCommit;
                        //failuresAccessor.DeleteWarning(failure);
                        //}
                        //else
                        //{
                        //TaskDialog.Show("error2",_failureMessage);
                        //}
                        //return FailureProcessingResult.ProceedWithRollBack;


                    }
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
