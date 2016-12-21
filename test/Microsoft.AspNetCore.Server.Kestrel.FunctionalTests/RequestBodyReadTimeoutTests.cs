// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestBodyReadTimeoutTests
    {
        [Fact]
        public async Task RequestTimesOutOnReadAsync()
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.RequestBodyReadTimeout = TimeSpan.FromSeconds(1);

            using (var server = new TestServer(async context =>
            {
                for (var i = 0; i < context.Request.ContentLength; i++)
                {
                    await context.Request.Body.ReadAsync(new byte[1], 0, 1);
                }
            }, serviceContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Content-Length: 42",
                        "",
                        new string('a', 41));
                    await ReceiveRequestTimeoutResponse(connection, server.Context.DateHeaderValue);
                }
            }
        }

        [Fact]
        public async Task RequestTimesOutOnCopyToAsync()
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.RequestBodyReadTimeout = TimeSpan.FromSeconds(1);

            using (var server = new TestServer(async context =>
            {
                var stream = new MemoryStream();
                await context.Request.Body.CopyToAsync(stream);
            }, serviceContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Content-Length: 42",
                        "",
                        new string('a', 41));
                    await ReceiveRequestTimeoutResponse(connection, server.Context.DateHeaderValue);
                }
            }
        }

        private async Task ReceiveRequestTimeoutResponse(TestConnection connection, string dateHeaderValue)
        {
            await connection.ReceiveForcedEnd(
                "HTTP/1.1 408 Request Timeout",
                "Connection: close",
                $"Date: {dateHeaderValue}",
                "Content-Length: 0",
                "",
                "").TimeoutAfter(TimeSpan.FromSeconds(10));
        }
    }
}
