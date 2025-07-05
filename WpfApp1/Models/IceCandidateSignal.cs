namespace WpfApp1.Models
{
    /// <summary>
    /// Represents an ICE candidate for signaling via Firebase.
    /// </summary>
    public class IceCandidateSignal
    {
        public string SdpMid { get; set; }
        public int SdpMLineIndex { get; set; }
        public string Candidate { get; set; }

        /// <summary>
        /// Converts this signal object to a WebRTC IceCandidate object.
        /// </summary>
        public Microsoft.MixedReality.WebRTC.IceCandidate ToWebRtcCandidate()
        {
            return new Microsoft.MixedReality.WebRTC.IceCandidate
            {
                SdpMid = this.SdpMid,
                SdpMlineIndex = this.SdpMLineIndex,
                Content = this.Candidate
            };
        }

        /// <summary>
        /// Creates a signal object from a WebRTC IceCandidate object.
        /// </summary>
        public static IceCandidateSignal FromWebRtcCandidate(Microsoft.MixedReality.WebRTC.IceCandidate candidate)
        {
            return new IceCandidateSignal
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMlineIndex,
                Candidate = candidate.Content
            };
        }
    }
}
