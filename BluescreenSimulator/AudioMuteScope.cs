
using System;
using System.Runtime.InteropServices;

namespace BluescreenSimulator
{
    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumeratorComObject
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(
            EDataFlow dataFlow, int stateMask, out IntPtr devices);

        void GetDefaultAudioEndpoint(
            EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        void Activate(
            ref Guid iid, int clsContext, IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object instance);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolume
    {
        void RegisterControlChangeNotify(IntPtr notify);
        void UnregisterControlChangeNotify(IntPtr notify);
        void GetChannelCount(out uint count);
        void SetMasterVolumeLevel(float level, ref Guid context);
        void SetMasterVolumeLevelScalar(float level, ref Guid context);
        void GetMasterVolumeLevel(out float level);
        void GetMasterVolumeLevelScalar(out float level);
        void SetChannelVolumeLevel(uint channel, float level, ref Guid context);
        void SetChannelVolumeLevelScalar(uint channel, float level, ref Guid context);
        void GetChannelVolumeLevel(uint channel, out float level);
        void GetChannelVolumeLevelScalar(uint channel, out float level);
        void SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid context);
        void GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }

    internal sealed class AudioMuteScope : IDisposable
    {
        private IMMDevice device;
        private IAudioEndpointVolume audio;
        private bool wasMuted;
        private bool disposed;

        public AudioMuteScope()
        {
            IMMDeviceEnumerator enumerator = null;

            try
            {
                enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
                Guid iid = typeof(IAudioEndpointVolume).GUID;
                device.Activate(ref iid, 23, IntPtr.Zero, out object instance);
                audio = (IAudioEndpointVolume)instance;
                audio.GetMute(out wasMuted);
                Guid context = Guid.Empty;
                audio.SetMute(true, ref context);
            }
            catch
            {
                ReleaseComObject(audio);
                ReleaseComObject(device);
                audio = null;
                device = null;
                throw;
            }
            finally
            {
                ReleaseComObject(enumerator);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            try
            {
                if (audio != null)
                {
                    Guid context = Guid.Empty;
                    audio.SetMute(wasMuted, ref context);
                }
            }
            finally
            {
                ReleaseComObject(audio);
                ReleaseComObject(device);
                audio = null;
                device = null;
            }
        }

        private static void ReleaseComObject(object value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.ReleaseComObject(value);
        }
    }
}
