// conversation.js - Chat functionality for conversation page

(function() {
    'use strict';
    
    const conversationId = window.conversationConfig?.conversationId;
    const userId = window.conversationConfig?.userId;
    const userName = window.conversationConfig?.userName;
    const isGroup = window.conversationConfig?.isGroup;

    let availableUsers = [];
    let showAllUsers = false;
    let connection = null;

    const messageInput = document.getElementById('messageInput');
    const sendButton = document.getElementById('sendButton');
    const messagesContainer = document.getElementById('messagesContainer');

    document.addEventListener('DOMContentLoaded', function() {
        initSignalR();
        initEventListeners();
        scrollToBottom();
        initAutoHideAlerts();
    });

    function initSignalR() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/chatHub?userId=" + userId)
            .withAutomaticReconnect()
            .build();

        connection.start()
            .then(() => {
                console.log("Connected to ChatHub");
                return connection.invoke("JoinConversation", conversationId);
            })
            .then(() => {
                console.log("Joined conversation:", conversationId);
            })
            .catch(err => console.error("SignalR connection error:", err));

        connection.on("ReceiveMessage", (message) => {
            console.log("Message received:", message);
            addMessageToUI(message);
            scrollToBottom();
        });
    }

    function initEventListeners() {
        if (messageInput) {
            messageInput.addEventListener('input', function() {
                this.style.height = 'auto';
                this.style.height = (this.scrollHeight) + 'px';
            });

            messageInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    sendMessage();
                }
            });
        }

        if (sendButton) {
            sendButton.addEventListener('click', sendMessage);
        }

        window.onclick = function(event) {
            if (event.target.classList.contains('modal')) {
                event.target.style.display = 'none';
            }
        };

        window.addEventListener('beforeunload', () => {
            if (connection) {
                connection.invoke("LeaveConversation", conversationId);
            }
        });
    }

    async function sendMessage() {
        if (!messageInput || !connection) return;
        
        const content = messageInput.value.trim();
        if (!content) return;

        sendButton.disabled = true;

        try {
            await connection.invoke("SendMessage", conversationId, content);
            messageInput.value = '';
            messageInput.style.height = 'auto';
        } catch (err) {
            console.error("Error sending message:", err);
            alert("Failed to send message. Please try again.");
        } finally {
            sendButton.disabled = false;
            messageInput.focus();
        }
    }

    function addMessageToUI(message) {
        if (!messagesContainer) return;
        
        const emptyState = messagesContainer.querySelector('.empty-messages');
        if (emptyState) {
            emptyState.remove();
        }

        const isSent = message.senderId === userId;
        const senderName = message.senderName || (isSent ? userName : "Unknown");
        const senderInitial = senderName.substring(0, 1).toUpperCase();
        const time = new Date(message.sentAt).toLocaleTimeString('en-US', { 
            hour: '2-digit', 
            minute: '2-digit',
            hour12: false 
        });

        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${isSent ? 'sent' : ''}`;
        messageDiv.innerHTML = `
            <div class="message-avatar">${escapeHtml(senderInitial)}</div>
            <div class="message-content">
                ${!isSent ? `<div class="message-sender">${escapeHtml(senderName)}</div>` : ''}
                <div class="message-text">${escapeHtml(message.content)}</div>
                <div class="message-time">${time}</div>
            </div>
        `;

        messagesContainer.appendChild(messageDiv);
    }

    function scrollToBottom() {
        if (messagesContainer) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    window.openModal = function(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) modal.style.display = 'block';
    };

    window.closeModal = function(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) modal.style.display = 'none';
    };

    window.openEditGroupModal = function() {
        openModal('editGroupModal');
    };

    window.openAddMemberModal = async function() {
        openModal('addMemberModal');
        await loadAvailableUsers(false);
    };

    async function loadAvailableUsers(showAll) {
        showAllUsers = showAll;
        try {
            const response = await fetch(`/Chat/GetAvailableUsers?conversationId=${conversationId}&showAll=${showAll}`);
            availableUsers = await response.json();
            displayUsers(availableUsers);
            updateFilterButtons();
        } catch (error) {
            console.error('Error loading users:', error);
        }
    }

    window.loadAvailableUsers = loadAvailableUsers;

    function updateFilterButtons() {
        const friendsBtn = document.getElementById('friendsFilterBtn');
        const allUsersBtn = document.getElementById('allUsersFilterBtn');
        
        if (!friendsBtn || !allUsersBtn) return;
        
        if (showAllUsers) {
            friendsBtn.classList.remove('active');
            allUsersBtn.classList.add('active');
        } else {
            friendsBtn.classList.add('active');
            allUsersBtn.classList.remove('active');
        }
    }

    function displayUsers(users) {
        const userList = document.getElementById('userList');
        if (!userList) return;

        userList.innerHTML = '';

        if (users.length === 0) {
            userList.innerHTML = `
                <p style="text-align: center; padding: 2rem; color: #999;">
                    ${showAllUsers ? 'No users available to add' : 'No friends available to add. Try "All Users".'}
                </p>
            `;
            return;
        }

        users.forEach(user => {
            const userItem = document.createElement('div');
            userItem.className = 'user-select-item';
            
            let avatarContent = '';
            if (user.profilePicture) {
                avatarContent = `<img src="${escapeHtml(user.profilePicture)}" alt="${escapeHtml(user.displayName || user.userName)}" style="width: 100%; height: 100%; object-fit: cover; border-radius: 50%;">`;
            } else {
                avatarContent = user.userName.substring(0, 1).toUpperCase();
            }
            
            userItem.innerHTML = `
                <div class="participant-avatar">
                    ${avatarContent}
                    ${user.isOnline ? '<span class="online-dot"></span>' : ''}
                </div>
                <div style="flex: 1; min-width: 0;">
                    <div class="participant-name" style="white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                        ${escapeHtml(user.displayName || user.userName)}
                        ${!showAllUsers ? '<span class="friend-badge">Friend</span>' : ''}
                    </div>
                    <div class="participant-role">${user.isOnline ? 'Online' : 'Offline'}</div>
                </div>
            `;
            userItem.onclick = () => addUserToGroup(user.id, user.displayName || user.userName);
            userList.appendChild(userItem);
        });
    }

    window.filterUsers = function() {
        const searchInput = document.getElementById('userSearch');
        if (!searchInput) return;
        
        const searchTerm = searchInput.value.toLowerCase();
        const filtered = availableUsers.filter(user => 
            (user.userName && user.userName.toLowerCase().includes(searchTerm)) ||
            (user.displayName && user.displayName.toLowerCase().includes(searchTerm))
        );
        displayUsers(filtered);
    };

    async function addUserToGroup(targetUserId, targetUserName) {
        if (!confirm(`Add ${targetUserName} to this group?`)) return;

        const formData = new FormData();
        formData.append('conversationId', conversationId);
        formData.append('userId', targetUserId);
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) formData.append('__RequestVerificationToken', token);

        try {
            const response = await fetch('/Chat/AddParticipant', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                closeModal('addMemberModal');
                location.reload();
            } else {
                const text = await response.text();
                alert('Failed to add participant: ' + text);
            }
        } catch (error) {
            console.error('Error adding participant:', error);
            alert('An error occurred while adding participant');
        }
    }

    window.removeMember = async function(targetUserId, targetUserName) {
        if (!confirm(`Remove ${targetUserName} from this group?`)) return;

        const formData = new FormData();
        formData.append('conversationId', conversationId);
        formData.append('userId', targetUserId);
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) formData.append('__RequestVerificationToken', token);

        try {
            const response = await fetch('/Chat/RemoveParticipant', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                location.reload();
            } else {
                const text = await response.text();
                alert('Failed to remove participant: ' + text);
            }
        } catch (error) {
            console.error('Error removing participant:', error);
            alert('An error occurred while removing participant');
        }
    };

    window.makeAdmin = async function(targetUserId, targetUserName) {
        if (!confirm(`Make ${targetUserName} an admin?`)) return;

        const formData = new FormData();
        formData.append('conversationId', conversationId);
        formData.append('userId', targetUserId);
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) formData.append('__RequestVerificationToken', token);

        try {
            const response = await fetch('/Chat/AssignAdmin', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                location.reload();
            } else {
                const text = await response.text();
                alert('Failed to assign admin: ' + text);
            }
        } catch (error) {
            console.error('Error assigning admin:', error);
            alert('An error occurred while assigning admin');
        }
    };

    window.leaveGroup = async function() {
        if (!confirm('Are you sure you want to leave this group?')) return;

        const formData = new FormData();
        formData.append('conversationId', conversationId);
        formData.append('userId', userId);
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) formData.append('__RequestVerificationToken', token);

        try {
            const response = await fetch('/Chat/LeaveGroup', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                window.location.href = '/Chat/Index';
            } else {
                const text = await response.text();
                alert('Failed to leave group: ' + text);
            }
        } catch (error) {
            console.error('Error leaving group:', error);
            alert('An error occurred while leaving the group');
        }
    };

    window.deleteConversation = async function() {
        const conversationType = isGroup ? 'group' : 'conversation';
        const message = isGroup 
            ? 'Are you sure you want to delete this group? This action cannot be undone and will remove the group for all members.' 
            : 'Are you sure you want to delete this conversation? This action cannot be undone and will remove the conversation for both users.';
        
        if (!confirm(message)) return;

        const formData = new FormData();
        formData.append('conversationId', conversationId);
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) formData.append('__RequestVerificationToken', token);

        try {
            const response = await fetch('/Chat/DeleteConversation', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                window.location.href = '/Chat/Index';
            } else {
                const text = await response.text();
                alert('Failed to delete ' + conversationType + ': ' + text);
            }
        } catch (error) {
            console.error('Error deleting conversation:', error);
            alert('An error occurred while deleting the ' + conversationType);
        }
    };

    function initAutoHideAlerts() {
        setTimeout(() => {
            const alerts = document.querySelectorAll('.alert');
            alerts.forEach(alert => {
                alert.style.opacity = '0';
                alert.style.transition = 'opacity 0.5s';
                setTimeout(() => alert.remove(), 500);
            });
        }, 5000);
    }
})();