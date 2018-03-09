using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{

    /// <summary>
    /// Status information for history Change nodes
    /// </summary>
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


    /// <summary>
    /// Base interaface for all Change nodes that can be passed to ChangeHistory (ie undo/redo operations)
    /// </summary>
    public interface IChangeOp
    {
        string Identifier();

        bool HasTags { get; }
        List<string> Tags { get; set; }     // get must not return null if HasTags is true

        OpStatus Apply();       // "redo" action
        OpStatus Revert();      // "undo" action
        OpStatus Cull();        // called when this operation is no longer needed, you should discard memory if possible
    }


    public delegate void OnChangeOpHandler(object source, IChangeOp op);


    /// <summary>
    /// Default implementation for changes, supports lazy-allocated tag set
    /// </summary>
    public abstract class BaseChangeOp : IChangeOp
    {
        public abstract string Identifier();
        public abstract OpStatus Apply();
        public abstract OpStatus Revert();
        public abstract OpStatus Cull();

        public virtual bool HasTags {
            get { return tags != null; }
        }

        public virtual List<string> Tags {
            get { if (tags == null) tags = new List<string>(); return tags; }
            set { tags = value; }
        }
        List<string> tags;

        public BaseChangeOp(bool bUseTags = false) {
            if (bUseTags)
                tags = new List<string>();
        }
    }

    /// <summary>
    /// special-case change operation, push one of these after interactive actions end
    /// and then you can use ChangeHistory.InteractiveStepBack/Forward to implement normal undo/redo-type interaction
    /// </summary>
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




    /// <summary>
    /// Top-level class for managing a linear stream of change/history events, ie implementations of IChangeOp.
    /// This is used to implement Undo/Redo functionality.
    /// The idea is that you push Changes, and then you can StepForward/StepBackward, and we call Apply()/Revert()
    /// on the Change nodes. To step forward/backward in chunks, Changes can be tagged with text strings.
    /// InteractionCheckpointOp is a special-case tagged node that you can use at the "end" of an interactive operation.
    /// </summary>
    public class ChangeHistory
    {
        List<IChangeOp> vHistory;       // stream of changes
        int iCurrent;                   // current position in vHistory


        public ChangeHistory()
        {
            vHistory = new List<IChangeOp>();
            iCurrent = 0;
        }


        /// <summary>
        /// History nodes can be tagged with text strings, this just generates a text string
        /// that should be unique for this History stream.
        /// </summary>
        /// <returns></returns>
        public string AllocateTag()
        {
            return "tag" + (iUniqueTagCounter++).ToString();
        }
        int iUniqueTagCounter = 1;

        /// <summary>
        /// If set to non-null/empty string, Active tag will be set on all pushed Changes
        /// </summary>
        public void SetActiveTag(string tag)
        {
            if (tag == null || tag.Length == 0)
                throw new Exception("ChangeHistory.SetActiveTag: invalid tag string");
            sForceSetTag = tag;
        }
        public void ClearActiveTag()
        {
            sForceSetTag = null;
        }
        string sForceSetTag = null;


        /// <summary>
        /// push Change onto the history stream. If we are not at end of stream, we truncate
        /// stream at the current point. If bIsApplied is false, we Apply() the new Change
        /// </summary>
        public OpStatus PushChange(IChangeOp op, bool bIsApplied = false)
        {
            DebugUtil.Log(4, "ChangeHistory.PushChange [{0}]: pushed {1}", iCurrent+1, op.Identifier());

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

            if (sForceSetTag != null)
                op.Tags.Add(sForceSetTag);
            vHistory.Add(op);
            iCurrent++;

            return OpStatus.Success;
        }

        /// <summary>
        /// PushChange with a new InteractionCheckpointOp
        /// </summary>
        public BaseChangeOp PushInteractionCheckpoint()
        {
            InteractionCheckpointOp op = new InteractionCheckpointOp();
            PushChange(op, true);
            return op;
        }


        /// <summary>
        /// If we are currently in a "past" state, we do not want to push repeat nodes.
        /// It isn't clear how to automatically prevent this, so you need to manually
        /// check that you are ! InPastState before pushing changes that you might have already pushed!!
        /// (ie if your Change.Apply() or Revert() calls something that would emit a new ChangeOp)
        /// </summary>
        public bool InPastState
        {
            get { return iCurrent < vHistory.Count; }
        }


        /// <summary>
        /// Rewind history back one Change
        /// </summary>
        public OpStatus StepBack()
        {
            if (iCurrent == 0)
                return OpStatus.Success;        // weird but ok

            IChangeOp op = vHistory[iCurrent - 1];
            DebugUtil.Log(4, "ChangeHistory.StepBack [{0}/{1}]: reverting {2}", iCurrent-1, vHistory.Count, op.Identifier());
            OpStatus result = op.Revert();
            if (result.code != OpStatus.no_error) {
                DebugUtil.Error("[ChangeHistory::StepBack] Revert() of ChangeOp {0} failed - result was code {1} message {2}",
                    op.Identifier(), result.code, result.message);
                return result;
            }

            iCurrent--;
            return OpStatus.Success;
        }

        /// <summary>
        /// Rewind history until current node has the given tag
        /// </summary>
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
                    IChangeOp op = vHistory[iCurrent - 1];
                    if (op.HasTags && op.Tags.Contains(tag))
                        bContinue = false;
                }
            }
            return OpStatus.Success;
        }

        /// <summary>
        /// Rewind history until current node has the interactive tag
        /// </summary>
        public OpStatus InteractiveStepBack()
        {
            return StepBackwardToAfterPreviousTag(InteractionCheckpointOp.InteractiveStateTag);
        }




        /// <summary>
        /// play history forward one Change
        /// </summary>
        public OpStatus StepForward()
        {
            if (iCurrent == vHistory.Count)
                return OpStatus.Success;

            IChangeOp op = vHistory[iCurrent];
            DebugUtil.Log(4, "ChangeHistory.StepForward [{0}/{1}]: applying {2}", iCurrent, vHistory.Count, op.Identifier());
            OpStatus result = op.Apply();
            if (result.code != OpStatus.no_error) {
                DebugUtil.Error("[ChangeHistory::StepForward] Apply() of ChangeOp {0} failed - result was code {1} message {2}",
                    op.Identifier(), result.code, result.message);
                return result;
            }

            iCurrent++;
            return OpStatus.Success;
        }

        /// <summary>
        /// play history forward until current node has the given tag
        /// </summary>
        public OpStatus StepForwardToAfterNextTag(string tag)
        {
            bool bContinue = true;
            bool bFound = false;
            while (bContinue) {
                OpStatus result = StepForward();
                if (result.code != OpStatus.no_error)
                    return result;
                if (iCurrent == vHistory.Count) {
                    bContinue = false;
                } else {
                    IChangeOp op = vHistory[iCurrent];
                    if (op.HasTags && op.Tags.Contains(tag)) {
                        bContinue = false;
                        bFound = true;
                    }
                }
            }
            // step to *after* this node, otherwise if we push a new node we will replace this one!
            // [TODO] this is maybe a bit weird...?
            if (bFound)
                StepForward();
            return OpStatus.Success;
        }

        /// <summary>
        /// play history forward until current node has the interactive tag
        /// </summary>
        public OpStatus InteractiveStepForward()
        {
            return StepForwardToAfterNextTag(InteractionCheckpointOp.InteractiveStateTag);
        }



        /// <summary>
        /// discard any nodes with given tag. Note that this could result in invalid/garbge history,
        /// preventing that is up to you.
        /// </summary>
        public void DiscardByTag(string tag)
        {
            for ( int i = 0; i < vHistory.Count; ++i ) {
                if (vHistory[i].HasTags == false)
                    continue;

                if ( vHistory[i].Tags.Contains(tag) ) {
                    if (i >= iCurrent)
                        throw new Exception("ChangeHistory.DiscardByTag: cannot discard current or future states!");
                    vHistory[i].Cull();
                    vHistory.RemoveAt(i);
                    i--;
                    iCurrent--;
                }
            }
        }



        /// <summary>
        /// Discard all future history nodes after current state. Called automatically
        /// when you push a node while at iCurrent < Count
        /// </summary>
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



        public void Clear()
        {
            for (int i = 0; i < vHistory.Count; ++i)
                vHistory[i].Cull();
            vHistory = new List<IChangeOp>();
            iCurrent = 0;
            FPlatform.SuggestGarbageCollection();
        }




        public void DebugPrint(string prefix = "HISTORY")
        {
            StringBuilder s = new StringBuilder();


            s.AppendLine(string.Format("[{0}] have {1} records, current is {2}", prefix, vHistory.Count, iCurrent));
            for (int i = vHistory.Count-1; i >= 0; --i) {
                string tags = "";
                if ( vHistory[i].HasTags ) {
                    foreach (string tag in vHistory[i].Tags)
                        tags += tag + " ";
                }
                string current = (i == iCurrent) ? " **" : "   ";

                s.AppendLine(string.Format("{0}[{1}] - {2} - tags: {3}", current, vHistory.Count-1-i, vHistory[i].Identifier(), tags));
            }

            DebugUtil.Log(1, s.ToString());
        }


    }
}
