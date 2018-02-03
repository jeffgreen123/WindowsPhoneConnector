using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Connect
{
    class Client
    {
       private Socket _socket = null;

        // Signaling object used to notify when an asynchronous operation is completed
        private static ManualResetEvent _clientDone = new ManualResetEvent(false);
        private byte[] picbytes;
        // Define a timeout in milliseconds for each asynchronous call. If a response is not received within this 
        // timeout period, the call is aborted.
        private const int TIMEOUT_MILLISECONDS = 5000;
        private int totalBytes;
        private bool picReady;
        public int percent;
        // The maximum size of the data buffer to use with the asynchronous socket methods
        public bool getPicReady()
        {
            return picReady;
        }
        public void setPicReady(bool value)
        {
            picReady = value;
        }
        public byte[] getPic()
        {
            return picbytes;
        }
        public string Connect(string hostname, int port)
        {
            string result = string.Empty;
            
            // Create DnsEndPoint. The hostName and port are passed in to this method.
            DnsEndPoint hostEntry = new DnsEndPoint("99.250.99.25", 27015);

            // Create a stream-based, TCP socket using the InterNetwork Address Family. 
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            // Create a SocketAsyncEventArgs object to be used in the connection request
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = hostEntry;

            // Inline event handler for the Completed event.
            // Note: This event handler was implemented inline in order to make this method self-contained.
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs ez)
            {
                // Retrieve the result of this request
                result = ez.SocketError.ToString();

                // Signal that the request is complete, unblocking the UI thread
                _clientDone.Set();
            });

            // Sets the state of the event to nonsignaled, causing threads to block
            _clientDone.Reset();

            // Make an asynchronous Connect request over the socket
            _socket.ConnectAsync(socketEventArg);

            // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
            // If no response comes back within this time then proceed
            percent = 0;
            _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
            return result;
        }
        public string Send(string data)
        {
            string response = "Operation Timeout";

            // We are re-using the _socket object initialized in the Connect method
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                // Set properties on context object
                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                socketEventArg.UserToken = null;

                // Inline event handler for the Completed event.
                // Note: This event handler was implemented inline in order 
                // to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    response = e.SocketError.ToString();

                    // Unblock the UI thread
                    _clientDone.Set();
                });

                // Add the data to be sent into the buffer
                byte[] payload = Encoding.UTF8.GetBytes(data);
                socketEventArg.SetBuffer(payload, 0, payload.Length);

                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();

                // Make an asynchronous Send request over the socket
                _socket.SendAsync(socketEventArg);
                
                
                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed
                _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
            }
            else
            {
                response = "Socket is not initialized";
            }

            return response;
        }
        public byte[] ReceiveImage(int size)
        {
            picbytes = new byte[size];
            picReady = false;
            string response = "Operation Timeout";
            // We are receiving over an established socket connection
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object

                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                
                // Setup the buffer to receive the data
                socketEventArg.SetBuffer(new Byte[size], 0, size);

                // Inline event handler for the Completed event.
                // Note: This even handler was implemented inline in order to make 
                // this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {

                        // Retrieve the data from the buffer
                        response = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                        int x = response.Length;
                        response = response.Trim('\0');

                    }
                    else
                    {
                        response = e.SocketError.ToString();
                        
                    }
                    _clientDone.Set();
                    totalBytes += e.BytesTransferred;
                    for (int i = 0; i < e.BytesTransferred; i++)
                    {
                        picbytes[i + totalBytes - e.BytesTransferred] = e.Buffer[i];
                    }
                    percent = totalBytes * 100 /size ;
                    if (totalBytes < size)
                    {

                        _socket.ReceiveAsync(e);
                    }
                    else
                    {
                        percent = 0;
                        totalBytes = 0;
                        picReady = true;
                    }
                });

                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();
                // Make an asynchronous Receive request over the socket
                _socket.ReceiveAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed
                _clientDone.WaitOne(TIMEOUT_MILLISECONDS);

            }
            else
            {
                response = "Socket is not initialized";
            }

            return socketEventArg.Buffer;
        }
  
        
        public string Receive(int size)
        {
            string response = "Operation Timeout";

            // We are receiving over an established socket connection
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object

                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;

                // Setup the buffer to receive the data
                socketEventArg.SetBuffer(new Byte[size], 0, size);

                // Inline event handler for the Completed event.
                // Note: This even handler was implemented inline in order to make 
                // this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        // Retrieve the data from the buffer
                        response = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                        response = response.Trim('\0');
                    }
                    else
                    {
                        response = e.SocketError.ToString();

                    }

                    _clientDone.Set();
                });

                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();
                // Make an asynchronous Receive request over the socket
                _socket.ReceiveAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed
                _clientDone.WaitOne(TIMEOUT_MILLISECONDS);

            }
            else
            {
                response = "Socket is not initialized";
            }

            return response;
        }

        /// <summary>
        /// Closes the Socket connection and releases all associated resources
        /// </summary>
        public void Close()
        {
            if (_socket != null)
            {
                _socket.Close();
            }
        }

    }
}
