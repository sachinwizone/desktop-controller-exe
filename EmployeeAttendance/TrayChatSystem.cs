using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;

namespace EmployeeAttendance
{
    /// <summary>
    /// Tray Chat System - In-app chat for peer-to-peer messaging between users
    /// </summary>
    public class TrayChatSystem
    {
        private ChatForm _chatForm;
        private string _currentUser;
        private string _companyName;
        private string _apiBaseUrl;

        public TrayChatSystem(string apiBaseUrl, string companyName, string currentUser)
        {
            _apiBaseUrl = apiBaseUrl;
            _companyName = companyName;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Show chat window
        /// </summary>
        public void ShowChatWindow()
        {
            try
            {
                if (_chatForm == null || _chatForm.IsDisposed)
                {
                    _chatForm = new ChatForm(_currentUser);
                }
                _chatForm.Show();
                _chatForm.BringToFront();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing chat: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup resources on exit
        /// </summary>
        public void Exit()
        {
            try
            {
                if (_chatForm != null && !_chatForm.IsDisposed)
                {
                    _chatForm.Close();
                    _chatForm.Dispose();
                }
            }
            catch { }
        }
    }
}
