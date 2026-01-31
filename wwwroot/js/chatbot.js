document.addEventListener('DOMContentLoaded', function () {
    const chatMessages = document.getElementById('chat-messages');
    const chatInput = document.getElementById('chat-input');
    const chatSendBtn = document.getElementById('chat-send-btn');

    // --- 1. HÀM GỬI TIN NHẮN VĂN BẢN ---
    async function sendMessage(text) {
        if (!text.trim()) return;
        appendMessage(text, 'user');
        chatInput.value = '';

        try {
            const response = await fetch('/Chat/Send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ text: text.trim() })
            });

            const data = await response.json();
            // Đảm bảo bóc tách đúng object response từ Python
            handleBotResponse(data.response || data);
        } catch (error) {
            console.error('Error:', error);
            appendMessage('Xin lỗi, chatbot đang bận.', 'bot');
        }
    }

    // --- 2. HÀM GỬI QUICK REPLY (ĐÃ FIX LỖI KHÔNG KHỚP) ---
    async function sendQuickReply(replyText) {
        // Làm sạch chuỗi trước khi gửi để Python so sánh chính xác 100%
        const cleanReply = replyText.trim();
        appendMessage(cleanReply, 'user');

        try {
            const response = await fetch('/Chat/QuickReply', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reply: cleanReply })
            });

            if (!response.ok) throw new Error('Lỗi phản hồi Quick Reply');

            const data = await response.json();

            // Lấy dữ liệu phản hồi từ Python (thường nằm trong data.response)
            const botData = data.response || data;
            handleBotResponse(botData);
        } catch (error) {
            console.error('QuickReply Error:', error);
            appendMessage('Không thể xử lý lựa chọn này.', 'bot');
        }
    }

    // --- 3. HÀM XỬ LÝ PHẢN HỒI TỪ BOT ---
    function handleBotResponse(res) {
        if (!res) return;

        // Hiển thị message (hỗ trợ cả viết hoa/thường từ API)
        const message = (res.response && res.response.message) || res.message || res.Message || "Mình chưa hiểu ý bạn...";
        appendMessage(message, 'bot');

        // Lấy danh sách options
        const options = (res.response && res.response.options) || res.options || res.Options || [];

        if (options.length > 0) {
            const optionsContainer = document.createElement('div');
            optionsContainer.className = 'flex flex-wrap gap-2 mt-2 ml-10 mb-4 animate-fade-in';

            options.forEach(opt => {
                const btn = document.createElement('button');
                btn.className = 'px-4 py-2 bg-blue-50 text-blue-700 rounded-full text-xs font-bold hover:bg-blue-600 hover:text-white transition-all border border-blue-200 shadow-sm active:scale-95';
                btn.innerText = opt;

                btn.onclick = () => {
                    const selectedOption = opt.trim(); // Loại bỏ khoảng trắng thừa
                    optionsContainer.remove();

                    // Log ra console để bạn kiểm tra xem chữ gửi đi là gì
                    console.log("Đang gửi lựa chọn:", selectedOption);

                    sendQuickReply(selectedOption);
                };
                optionsContainer.appendChild(btn);
            });
            chatMessages.appendChild(optionsContainer);
        }
        scrollToBottom();
    }

    // --- 4. HIỂN THỊ TIN NHẮN ---
    function appendMessage(text, sender) {
        if (!text) return;
        const msgWrapper = document.createElement('div');
        msgWrapper.className = sender === 'user' ? 'flex justify-end mb-4' : 'flex gap-3 mb-4';

        const contentClass = sender === 'user'
            ? 'bg-blue-600 text-white p-3 rounded-2xl rounded-tr-none text-sm shadow-md max-w-[75%]'
            : 'bg-white text-gray-800 p-3 rounded-2xl rounded-tl-none shadow-md text-sm max-w-[75%] border border-gray-100';

        const avatarHtml = sender === 'bot'
            ? `<div class="w-9 h-9 rounded-full bg-gradient-to-tr from-blue-500 to-indigo-600 flex items-center justify-center text-white shadow-lg flex-shrink-0"><i class="fa-solid fa-robot text-sm"></i></div>`
            : '';

        msgWrapper.innerHTML = `
            ${avatarHtml}
            <div class="${contentClass}">
                ${text.toString().replace(/\n/g, '<br>')}
            </div>
        `;

        chatMessages.appendChild(msgWrapper);
        scrollToBottom();
    }

    function scrollToBottom() {
        chatMessages.scrollTo({ top: chatMessages.scrollHeight, behavior: 'smooth' });
    }

    if (chatSendBtn) chatSendBtn.addEventListener('click', () => sendMessage(chatInput.value));
    if (chatInput) chatInput.addEventListener('keypress', (e) => { if (e.key === 'Enter') sendMessage(chatInput.value); });
});