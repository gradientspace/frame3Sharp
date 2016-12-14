using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public class Message
    {
        public enum Types
        {
            GenericMessage,
            Suggestion,
            Warning,
            Error
        }
        public Types Type { get; set; }
        public string Code { get; set; }
        public string Text { get; set; }
    }

    public enum MessageHandlerResult
    {
        MessageIgnored,
        MessageHandled_Keep,
        MessageHandled_Remove
    }


    public interface MessageHandler
    {
        bool Matches(Message m);
        MessageHandlerResult Handle(Message m);
    }

    public class DelegateMessageHandler : MessageHandler
    {
        Func<Message,bool> matchF;
        Func<Message, MessageHandlerResult> handleF;

        public DelegateMessageHandler(Func<Message, bool> matchF, Func<Message, MessageHandlerResult> handleF)
        {
            this.matchF = matchF;
            this.handleF = handleF;
        }

        public bool Matches(Message m)
        {
            return matchF(m);
        }
        public MessageHandlerResult Handle(Message m)
        {
            return handleF(m);
        }
    }


    public class MessageStream
    {
        List<Message> vMessages;
        public List<Message> Messages { get { return vMessages; } }

        List<MessageHandler> vHandlers;

        private MessageStream()
        {
            vMessages = new List<Message>();
            vHandlers = new List<MessageHandler>();
        }
        static private MessageStream singleton;
        static public MessageStream Get {
            get { if (singleton == null)
                    singleton = new MessageStream();
                  return singleton; }
        }


        public void RegisterHandler(MessageHandler h)
        {
            vHandlers.Add(h);
        }


        public void AddMessage(Message m)
        {
            vMessages.Add(m);
            foreach ( var h in vHandlers ) {
                if (h.Matches(m)) {
                    var r = h.Handle(m);
                    if (r == MessageHandlerResult.MessageHandled_Remove) {
                        vMessages.Remove(m);
                        return;
                    }
                }
            }
        }

        public void RemoveMessage(Message m)
        {
            vMessages.Remove(m);
        }


    }
}
