using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public class ThreadedJob
    {
        private bool m_IsDone = false;
        private object m_Handle = new object();
        private Thread m_Thread = null;

        public bool IsDone
        {
            get
            {
                bool tmp;
                lock (m_Handle)
                {
                    tmp = m_IsDone;
                }
                return tmp;
            }
            set
            {
                lock (m_Handle)
                {
                    m_IsDone = value;
                }
            }
        }

        public virtual void Start()
        {
            m_Thread = new Thread(Run);
            m_Thread.Start();
        }
        public virtual void Abort()
        {
            m_Thread.Abort();
        }
        
        protected virtual void ThreadFunction()
        {
            // Do your threaded task.
            // DON'T use the Unity API here.
            // Unity is not thread safe.
        }
        
        protected virtual void OnFinished()
        {
            // This is executed by the Unity main thread when the job is finished
        }
        
        public virtual bool Update()
        {
            if (IsDone)
            {
                OnFinished();
                return true;
            }
            return false;
        }
        public IEnumerator WaitFor()
        {
            while (!Update())
            {
                yield return null;
            }
        }
        private void Run()
        {
            ThreadFunction();
            IsDone = true;
        }
    }
}
