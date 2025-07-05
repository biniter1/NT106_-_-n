using System;

namespace WpfApp1.Models
{
    public class CallSignal
    {
        public string CallId { get; set; }
        public string CallerId { get; set; }
        public string CallerName { get; set; }
        public string CallerAvatarUrl { get; set; }
        public string CalleeId { get; set; }
        public string CallType { get; set; } // "Video" or "Voice"
        public string Status { get; set; } // "Ringing", "Accepted", "Rejected", "Ended", "Busy"
        public DateTime Timestamp { get; set; }

        // Các trường cho WebRTC signaling sau này
        public string SdpOffer { get; set; }
        public string SdpAnswer { get; set; }
    }
}
