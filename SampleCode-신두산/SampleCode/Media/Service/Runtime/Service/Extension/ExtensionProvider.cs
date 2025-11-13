using System;

namespace  UCF.Media.Service
{
    internal class ExtensionProvider : IDisposable
    {
        public BaseProvider provider;
        public LandMediaData mediaData;

        public Action<int> onNotifyChangeMediaState;
        public Action<double, double> onNotifyChangeMediaProgress;

        public ExtensionProvider(BaseProvider p, Action<int> c1, Action<double, double> c2)
        {
            provider = p;
            mediaData = new LandMediaData(0);

            onNotifyChangeMediaState = c1;
            onNotifyChangeMediaProgress = c2;
        }

        public void Dispose()
        {
            provider.Dispose();
            provider = null;

            mediaData = null;

            onNotifyChangeMediaState = null;
            onNotifyChangeMediaProgress = null;
        }
    }
}