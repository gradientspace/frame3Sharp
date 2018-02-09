using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace f3
{

    /// <summary>
    /// This is equivalent to lock(object), but you can pass it around.
    /// The lock is not released until Dispose() is claled.
    /// Which is, of course, incredibly dangerous. 
    /// However there are places where for performance reasons this risk is necessary,
    /// like allowing external code to modify DMeshSO.Mesh, to avoid huge mesh copies.
    /// 
    /// You should use it like this:
    /// 
    /// using(var locked = SomeClass.GetExternalLock()) {
    ///    // do your thing that is so important
    /// }
    /// 
    /// If you aren't using it like that, then you probably shouldn't be using it!
    /// </summary>
    public class DangerousExternalLock : IDisposable
    {
        public static DangerousExternalLock Lock(object o, Action onUnlockF)
        {
            DangerousExternalLock l = new DangerousExternalLock(o) {
                OnUnlockedF = onUnlockF
            };
            Monitor.Enter(o);
            return l;
        }

        private object target;
        private Action OnUnlockedF = null;
        private DangerousExternalLock(object o) {
            target = o;
        }
        
        public void Dispose()
        {
            Monitor.Exit(target);
            if (OnUnlockedF != null)
                OnUnlockedF();
        }
    }



    /// <summary>
    /// TimdLock times out if a lock cannot be acquired. 
    /// This is safer, ie can prevent deadlocks
    /// Note that this is a struct.
    /// </summary>
    public struct TimedLock : IDisposable
    {
        public static TimedLock Lock (object o, int seconds) {
            return Lock (o, TimeSpan.FromSeconds(seconds));
        }

        public static TimedLock Lock (object o, TimeSpan timeout) {
            TimedLock tl = new TimedLock (o);
            if ( ! Monitor.TryEnter (o, timeout) )  
                throw new LockTimeoutException ();
            return tl;
        }

        private TimedLock (object o) {
            target = o;
        }
        private object target;

        public void Dispose () {
            Monitor.Exit (target);
        }
    }
    public class LockTimeoutException : ApplicationException {
        public LockTimeoutException () : base("Timeout waiting for lock") {
        }
        public LockTimeoutException(string message) : base(message) {
        }
    }

}
