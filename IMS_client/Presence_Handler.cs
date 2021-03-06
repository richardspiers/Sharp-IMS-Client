﻿using System;
using System.Windows;
using System.Xml.Linq;
using SIPLib.SIP;

namespace IMS_client
{
    class PresenceHandler
    {

        public class PresenceChangedArgs : EventArgs
        {
            public string Contact;
            public string Basis;
            public string Note;

            public PresenceChangedArgs(string contact, string basis, string note)
            {
                Contact = contact;
                Basis = basis;
                Note = note;
            }
        }

        readonly SIPApp _app;

        public event EventHandler<SipMessageEventArgs> PresenceResponseEvent;
        public event EventHandler<PresenceChangedArgs> PresenceChangedEvent;

        public PresenceHandler(SIPApp app)
        {
            _app = app;
        }

        public void Subscribe(string sipUri)
        {
            UserAgent pua = new UserAgent(_app.Stack)
                                {RemoteParty = new Address(sipUri), LocalParty = _app.RegisterUA.LocalParty};
            Message request = pua.CreateRequest("SUBSCRIBE");
            request.InsertHeader(new Header("presence", "Event"));
            pua.SendRequest(request);
        }
       
        public void ProcessRequest(Message request)
        {
            if (request.Method.ToUpper().Contains("NOTIFY"))
            {
                if (request.Headers.ContainsKey("Content-Length"))
                {
                    if (request.Body.Length > 0)
                    {
                        try
                        {
                            XDocument xDoc = XDocument.Parse(request.Body.Trim());
                            string basic = "";
                            string note = "";
                            foreach (XElement xElement in xDoc.Descendants())
                            {
                                switch (xElement.Name.ToString())
                                {
                                    case "{urn:ietf:params:xml:ns:pidf}basic":
                                        basic = xElement.Value;
                                        break;

                                    case "{urn:ietf:params:xml:ns:pidf}note":
                                        note = xElement.Value;
                                        break;
                                }
                            }
                            if (PresenceChangedEvent != null)
                            {
                                string contact = request.First("From").Value.ToString();
                                contact = contact.Replace("<","");
                                contact = contact.Replace(">","");
                                PresenceChangedEvent(this, new PresenceChangedArgs(contact, basic, note));
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show("Error in handling presence xml: " + exception.Message);
                        }
                    }

                }

            }
        }

        public void Publish(string sipUri, string basic, string note,int expires)
        {
            _app.Publish(sipUri, basic, note, expires);
        }
    }
}
