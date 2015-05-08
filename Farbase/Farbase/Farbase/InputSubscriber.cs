using System.Collections.Generic;

namespace Farbase
{
    public interface IInputSubscriber
    {
        void ReceiveInput(string s);
    }

    public class InputSubscriber
    {
        public static List<InputSubscriber> Subscribers =
            new List<InputSubscriber>();

        protected IInputSubscriber subscriber;
        private List<string> subscriptions;

        //we can probably remove engine from this by simply
        //supplying an engine reference in update
        public InputSubscriber(IInputSubscriber sub)
        {
            subscriber = sub;
            subscriptions = new List<string>();
        }

        public void Register()
        {
            if (!Subscribers.Contains(this))
                Subscribers.Add(this);
        }

        public void Unregister()
        {
            Subscribers.Remove(this);
        }

        public void Update(fbEngine engine)
        {
            if (subscriptions == null)
                return;

            foreach (string binding in subscriptions)
            {
                if (binding[0] == '+')
                {
                    string _binding = binding.Substring(1, binding.Length - 1);
                    if (engine.BindingHeld(_binding))
                        subscriber.ReceiveInput(binding);
                }
                else
                {
                    if (engine.BindingPressed(binding))
                        subscriber.ReceiveInput(binding);
                }
            }
        }

        public void UnsubscribeAll()
        {
            subscriptions.Clear();
        }

        public void Unsubscribe(string s)
        {
            subscriptions.Remove(s.ToLower());
        }

        public InputSubscriber Subscribe(string s)
        {
            subscriptions.Add(s.ToLower());
            return this;
        }
    }
}