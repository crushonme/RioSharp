﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RioSharp
{
    public class RioTcpListener : RioSocketPoolBase, IDisposable
    {
        internal IntPtr _listenerSocket;

        public unsafe RioTcpListener(RioFixedBufferPool sendPool, RioFixedBufferPool revicePool) : base(sendPool, revicePool)
        {
            if ((_listenerSocket = Imports.WSASocket(ADDRESS_FAMILIES.AF_INET, SOCKET_TYPE.SOCK_STREAM, PROTOCOL.IPPROTO_TCP, IntPtr.Zero, 0, SOCKET_FLAGS.REGISTERED_IO | SOCKET_FLAGS.OVERLAPPED)) == IntPtr.Zero)
                Imports.ThrowLastWSAError();
        }

        public void Bind(IPEndPoint localEP)
        {
            in_addr inAddress = new in_addr();
            inAddress.s_b1 = localEP.Address.GetAddressBytes()[0];
            inAddress.s_b2 = localEP.Address.GetAddressBytes()[1];
            inAddress.s_b3 = localEP.Address.GetAddressBytes()[2];
            inAddress.s_b4 = localEP.Address.GetAddressBytes()[3];

            sockaddr_in sa = new sockaddr_in();
            sa.sin_family = ADDRESS_FAMILIES.AF_INET;
            sa.sin_port = Imports.htons((ushort)localEP.Port);
            Imports.ThrowLastWSAError();
            sa.sin_addr = inAddress;

            unsafe
            {
                if (Imports.bind(_listenerSocket, ref sa, sizeof(sockaddr_in)) == Imports.SOCKET_ERROR)
                    Imports.ThrowLastWSAError();
            }
        }

        public void Listen(int backlog)
        {
            if (Imports.listen(_listenerSocket, backlog) == Imports.SOCKET_ERROR)
                Imports.ThrowLastWSAError();
        }

        public RioTcpConnection Accept()
        {
            unsafe
            {
                sockaddr_in sa = new sockaddr_in();
                int len = sizeof(sockaddr_in);
                IntPtr accepted = Imports.accept(_listenerSocket, ref sa, ref len);
                if (accepted == new IntPtr(-1))
                    Imports.ThrowLastWSAError();

                var res = new RioTcpConnection(accepted, this);
                connections.TryAdd(res.GetHashCode(), res);
                return res;
            }
        }
    }
}