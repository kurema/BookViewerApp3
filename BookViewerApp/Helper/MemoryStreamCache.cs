﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace BookViewerApp.Helper
{
    public class MemoryStreamCache : IDisposable
    {
        private System.Threading.SemaphoreSlim Semaphore = new System.Threading.SemaphoreSlim(1, 1);
        private System.Threading.SemaphoreSlim SemaphoreProvider = new System.Threading.SemaphoreSlim(1, 1);

        private MemoryStream ContentCache;

        public Func<MemoryStreamCache, Task<MemoryStream>> MemoryStreamProvider;

        public async Task<MemoryStream> GetMemoryStreamByProviderAsync()
        {
            await SemaphoreProvider.WaitAsync();
            try
            {
                var ms = ContentCache ?? await MemoryStreamProvider?.Invoke(this);
                if (ms == null) return null;
                ms.Seek(0, SeekOrigin.Begin);
                return ContentCache = ms;
            }
            finally
            {
                SemaphoreProvider.Release();
            }
        }

        public async Task<MemoryStream> GetMemoryStreamAsync(Stream stream)
        {
            if (ContentCache != null)
            {
                ContentCache.Seek(0, SeekOrigin.Begin);
                return ContentCache;
            }

            await Semaphore.WaitAsync();
            try
            {
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return ContentCache = ms;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        protected virtual void Dispose(bool managed)
        {
            if (managed)
            {
                ContentCache?.Dispose();
                ContentCache = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
