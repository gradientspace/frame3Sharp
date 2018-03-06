using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace f3
{
    /// <summary>
    /// This class has a cutesy name because I want it to stick out. 
    /// You can use it to post "messages" between threads, in the form of Actions.
    /// Primarily added to allow background threads to do things on the Main UI thread,
    /// like create SceneObjects, etc. 
    /// However, you could potentially use it for other things
    /// </summary>
    public static class ThreadMailbox
    {

        public static void PostToMainThread(Action a)
        {
            if (a == null) {
                DebugUtil.Log("ThreadMailbox.PostToMainThread: tried to post null action!");
                return;
            }

            PostToThread(MainThreadName, a);
        }
        public static void PostToThread(string threadName, Action a)
        {
            if (a == null) {
                DebugUtil.Log("ThreadMailbox.PostToMainThread: tried to post null action!");
                return;
            }

            lock (Directory) {
                Mailbox box = _locked_get_mailbox(threadName);
                box.PendingActions.Add(a);
            }
        }


        public static void ProcessMainThreadMail()
        {
            ProcessThreadMail(MainThreadName);
        }
        public static void ProcessThreadMail(string threadName)
        {
            lock(Directory) {
                Mailbox box = _locked_get_mailbox(threadName);
                foreach (var a in box.PendingActions)
                    a();
                box.PendingActions.Clear();
            }
        }


        public static List<Action> ExtractThreadMail(string threadName)
        {
            List<Action> result = new List<Action>();
            lock(Directory) {
                Mailbox box = _locked_get_mailbox(threadName);
                result.AddRange(box.PendingActions);
                box.PendingActions.Clear();
            }
            return result;
        }



        //
        // internals
        //

        const string MainThreadName = "Main";

        class Mailbox
        {
            public List<Action> PendingActions = new List<Action>();
        }

        static Dictionary<string, Mailbox> Directory = new Dictionary<string, Mailbox>();

        // DO NOT CALL THIS W/O HOLDING LOCK ON DIRECTORY
        static Mailbox _locked_get_mailbox(string name)
        {
            if ( Directory.ContainsKey(name) == false ) {
                Mailbox newBox = new Mailbox();
                Directory[name] = newBox;
            }
            return Directory[name];
        }

    }
}
