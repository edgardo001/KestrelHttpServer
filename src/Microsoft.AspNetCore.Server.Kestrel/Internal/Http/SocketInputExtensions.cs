// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public static class SocketInputExtensions
    {
        public static ValueTask<int> ReadAsync(this SocketInput input, byte[] buffer, int offset, int count)
        {
            while (input.IsCompleted)
            {
                var fin = input.CheckFinOrThrow();

                var begin = input.ConsumingStart();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.ConsumingComplete(end, end);

                if (actual != 0 || fin)
                {
                    return new ValueTask<int>(actual);
                }
            }

            return new ValueTask<int>(input.ReadAsyncAwaited(buffer, offset, count));
        }

        private static async Task<int> ReadAsyncAwaited(this SocketInput input, byte[] buffer, int offset, int count)
        {
            while (true)
            {
                await input;

                var fin = input.CheckFinOrThrow();

                var begin = input.ConsumingStart();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.ConsumingComplete(end, end);

                if (actual != 0 || fin)
                {
                    return actual;
                }
            }
        }

        public static ValueTask<ArraySegment<byte>> PeekAsync(this SocketInput input, CancellationToken cancellationToken = default(CancellationToken))
        {
            while (input.IsCompleted)
            {
                var fin = input.CheckFinOrThrow();

                var begin = input.ConsumingStart();
                var segment = begin.PeekArraySegment();
                input.ConsumingComplete(begin, begin);

                if (segment.Count != 0 || fin)
                {
                    return new ValueTask<ArraySegment<byte>>(segment);
                }
            }

            return new ValueTask<ArraySegment<byte>>(input.PeekAsyncAwaited(cancellationToken));
        }

        private static async Task<ArraySegment<byte>> PeekAsyncAwaited(this SocketInput input, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.Register((state) =>
                {
                    var self = (SocketInput)state;
                    self.CompleteAwaiting();
                }, input);

                await input;

                cancellationToken.ThrowIfCancellationRequested();

                var fin = input.CheckFinOrThrow();

                var begin = input.ConsumingStart();
                var segment = begin.PeekArraySegment();
                input.ConsumingComplete(begin, begin);

                if (segment.Count != 0 || fin || cancellationToken.IsCancellationRequested)
                {
                    return segment;
                }
            }
        }
    }
}
