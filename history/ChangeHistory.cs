using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{

    public struct OpStatus
    {
        public const int no_error = 0;
        public const int generic_error = 1;

        public int code;
        public string message;

        public OpStatus(int nError, string sMessage = "") {
            this.code = nError;
            this.message = sMessage;
        }

        public static readonly OpStatus Success = new OpStatus(no_error, "");

        public static OpStatus Failed(string sMessage, int nCustomCode = 1) {
            return new OpStatus(nCustomCode, sMessage);
        }
    }


    public interface IChangeOp
    {
        string Identifier();
        List<string> GetTags();

        OpStatus Apply();
        OpStatus Revert();
        OpStatus Cull();        // called when this operation is no longer needed
    }


    public delegate void OnChangeOpHandler(object source, IChangeOp op);


    public abstract class BaseChangeOp : IChangeOp
    {
        public abstract string Identifier();
        public abstract OpStatus Apply();
        public abstract OpStatus Revert();
        public abstract OpStatus Cull();

        public virtual List<string> GetTags() { return Tags; }
        public List<string> Tags { get; set; }

        public BaseChangeOp(bool bUseTags = false) {
            if (bUseTags)
                Tags = new List<string>();
        }
    }

    public class InteractionCheckpointOp : BaseChangeOp
    {
        public const string InteractiveStateTag = "//Interactive";

        public override string Identifier() { return "InteractionCheckpoint"; }
        public override OpStatus Apply() { return OpStatus.Success; }
        public override OpStatus Revert() { return OpStatus.Success; }
        public override OpStatus Cull() { return OpStatus.Success; }

        public InteractionCheckpointOp() : base(true)
        {
            Tags.Add(InteractiveStateTag);
        }
    }




    public class ChangeHistory
    {
        List<IChangeOp> vHistory;
        int iCurrent;

        public ChangeHistory()
        {
            vHistory = new List<IChangeOp>();
            iCurrent = 0;
        }


        public OpStatus PushChange(IChangeOp op, bool bIsApplied = false)
        {
            DebugUtil.Log(2, "ChangeHistory.PushChange: pushed {0}", op.Identifier());

            if (vHistory.Count > 0 && iCurrent < vHistory.Count)
                TrimFuture();

            if (bIsApplied == false) {
                OpStatus result = op.Apply();
                if (result.code != OpStatus.no_error) {
                    DebugUtil.Error("[ChangeHistory::PushChange] Apply() of ChangeOp {0} failed - code {1} message {2}", 
                        op.Identifier(), result.code, result.message);
                    return result;
                }
            }

            vHistory.Add(op);
            iCurrent++;

            return OpStatus.Success;
        }

        public void PushInteractionCheckpoint()
        {
            PushChange(new InteractionCheckpointOp(), true);
        }


        /// <summary>
        /// If we are currently in a stepback state, we do not want to push repeat nodes.
        /// It isn't clear how to automatically prevent this, so you need to manually
        /// check that you are ! InPastState before pushing changes that you might have already pushed!!
        /// </summary>
        public bool InPastState
        {
            get { return iCurrent < vHistory.Count; }
        }


        public OpStatus StepBack()
        {
            if (iCurrent == 0)
                return OpStatus.Success;        // weird but ok

            IChangeOp op = vHistory[iCurrent - 1];
            DebugUtil.Log(2, "ChangeHistory.StepBack: reverting {0}", op.Identifier());
            OpStatus result = op.Revert();
            if (result.code != OpStatus.no_error) {
                DebugUtil.Error("[ChangeHistory::StepBack] Revert() of ChangeOp {0} failed - result was code {1} message {2}",
                    op.Identifier(), result.code, result.message);
                return result;
            }

            iCurrent--;
            return OpStatus.Success;
        }
        public OpStatus StepBackwardToAfterPreviousTag(string tag)
        {
            bool bContinue = true;
            while (bContinue) {
                OpStatus result = StepBack();
                if (result.code != OpStatus.no_error)
                    return result;
                if (iCurrent == 0) {
                    bContinue = false;
                } else {
                    var tags = vHistory[iCurrent - 1].GetTags();
                    if (tags != null && tags.Contains(tag))
                        bContinue = false;
                }
            }
            return OpStatus.Success;
        }

        public OpStatus InteractiveStepBack()
        {
            return StepBackwardToAfterPreviousTag(InteractionCheckpointOp.InteractiveStateTag);
        }





        public OpStatus StepForward()
        {
            if (iCurrent == vHistory.Count)
                return OpStatus.Success;

            IChangeOp op = vHistory[iCurrent];
            DebugUtil.Log(2, "ChangeHistory.StepForward: applying {0}", op.Identifier());
            OpStatus result = op.Apply();
            if (result.code != OpStatus.no_error) {
                DebugUtil.Error("[ChangeHistory::StepForward] Apply() of ChangeOp {0} failed - result was code {1} message {2}",
                    op.Identifier(), result.code, result.message);
                return result;
            }

            iCurrent++;
            return OpStatus.Success;
        }
        public OpStatus StepForwardToAfterNextTag(string tag)
        {
            bool bContinue = true;
            while (bContinue) {
                OpStatus result = StepForward();
                if (result.code != OpStatus.no_error)
                    return result;
                if (iCurrent == vHistory.Count) {
                    bContinue = false;
                } else {
                    var tags = vHistory[iCurrent].GetTags();
                    if (tags != null && tags.Contains(tag))
                        bContinue = false;
                }
            }
            return OpStatus.Success;
        }
        public OpStatus InteractiveStepForward()
        {
            return StepForwardToAfterNextTag(InteractionCheckpointOp.InteractiveStateTag);
        }




        void TrimFuture()
        {
            if (iCurrent < vHistory.Count) {
                for (int i = iCurrent; i < vHistory.Count; ++i)
                    vHistory[i].Cull();
                vHistory.RemoveRange(iCurrent, vHistory.Count - iCurrent);
            }
            if (iCurrent > vHistory.Count)
                throw new Exception("ChangeHistory.TrimFuture: iCurrent points into non-existent future!");
        }


    }
}
