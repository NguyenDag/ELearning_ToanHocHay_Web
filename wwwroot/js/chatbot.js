// Đảm bảo Script chỉ chạy sau khi toàn bộ tài liệu đã tải xong
document.addEventListener('DOMContentLoaded', function () {
    const chatMessages = document.getElementById('chat-messages');
    const chatInput = document.getElementById('chat-input');
    const chatSendBtn = document.getElementById('chat-send-btn');

    // 1. Hàm gửi tin nhắn văn bản
    async function sendMessage(text) {
        if (!text.trim()) return;

        appendMessage(text, 'user');
        chatInput.value = '';

        try {
            const response = await fetch('/Chat/Send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ text: text })
            });

            if (!response.ok) throw new Error('Network response was not ok');

            const data = await response.json();
            handleBotResponse(data.response);
        } catch (error) {
            console.error('Error:', error);
            appendMessage('Xin lỗi, đã có lỗi kết nối xảy ra.', 'bot');
        }
    }

    // 2. Hàm gửi Quick Reply (nút bấm)
    async function sendQuickReply(replyText) {
        appendMessage(replyText, 'user');

        try {
            const response = await fetch('/Chat/QuickReply', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reply: replyText })
            });

            if (!response.ok) throw new Error('Network response was not ok');

            const data = await response.json();
            handleBotResponse(data.response);
        } catch (error) {
            console.error('Error:', error);
        }
    }

    // 3. Hàm xử lý phản hồi từ Bot
    function handleBotResponse(res) {
        appendMessage(res.message, 'bot');

        // Vẽ các nút bấm nếu có options
        if (res.options && res.options.length > 0) {
            const optionsContainer = document.createElement('div');
            optionsContainer.className = 'flex flex-wrap gap-2 mt-2 ml-10';

            res.options.forEach(opt => {
                const btn = document.createElement('button');
                btn.className = 'px-3 py-1.5 bg-blue-100 text-primary rounded-full text-xs font-bold hover:bg-primary hover:text-white transition-all border border-blue-200';
                btn.innerText = opt;
                btn.onclick = () => {
                    optionsContainer.remove();
                    sendQuickReply(opt);
                };
                optionsContainer.appendChild(btn);
            });
            chatMessages.appendChild(optionsContainer);
        }

        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    // 4. Hàm hiển thị tin nhắn lên UI
    function appendMessage(text, sender) {
        const msgDiv = document.createElement('div');
        msgDiv.className = sender === 'user' ? 'flex justify-end' : 'flex gap-2';

        const contentClass = sender === 'user'
            ? 'bg-primary text-white p-3 rounded-2xl rounded-tr-none text-sm shadow-sm max-w-[80%]'
            : 'bg-white text-gray-700 p-3 rounded-2xl rounded-tl-none shadow-sm text-sm max-w-[80%]';

        const avatarHtml = sender === 'bot'
            ? `<div class="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center text-primary text-xs flex-shrink-0"><i class="fa-solid fa-robot"></i></div>`
            : '';

        msgDiv.innerHTML = `${avatarHtml}<div class="${contentClass}">${text}</div>`;
        chatMessages.appendChild(msgDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    // 5. Gán sự kiện cho Nút gửi và phím Enter
    if (chatSendBtn) {
        chatSendBtn.addEventListener('click', function () {
            sendMessage(chatInput.value);
        });
    }

    if (chatInput) {
        chatInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                sendMessage(chatInput.value);
            }
        });
    }
});