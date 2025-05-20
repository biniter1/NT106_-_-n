using CommunityToolkit.Mvvm.ComponentModel;
using WpfApp1.Models; // Namespace chứa FriendRequest Model
using System;

namespace WpfApp1.ViewModels
{
    public partial class FriendRequestViewModel : ObservableObject
    {
        private readonly FriendRequest _requestModel; // Giữ tham chiếu đến Model

        // Properties expose dữ liệu từ Model
        public string RequestId => _requestModel.EmailRequestId;
        public string RequesterId => _requestModel.EmailRequesterId;
        public string RequesterName => _requestModel.RequesterName;
        public string RequesterAvatarUrl => _requestModel.RequesterAvatarUrl;
        public DateTime RequestTime => _requestModel.RequestTime;

        public FriendRequestViewModel(FriendRequest requestModel)
        {
            _requestModel = requestModel ?? throw new ArgumentNullException(nameof(requestModel));
        }

        public FriendRequest GetModel() => _requestModel;

        public override string ToString()
        {
            // Giả sử FriendRequest Model có ToString() hoặc bạn tự định dạng
            return $"RequestVM from: {RequesterName} ({RequestId})";
        }
    }
}