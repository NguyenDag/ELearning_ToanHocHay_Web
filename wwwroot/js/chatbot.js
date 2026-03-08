document.addEventListener('DOMContentLoaded', function () {
    const chatMessages = document.getElementById('chat-messages');
    const chatInput = document.getElementById('chat-input');
    const chatSendBtn = document.getElementById('chat-send-btn');

    // --- TRIGGER TRACKING ---
    let hasUserInteracted = false;
    let triggersSent = {
        page_load: false,
        wait_15s: false,
        scroll_70: false
    };

    // --- 0. TRIGGER: PAGE LOAD ---
    async function triggerPageLoad() {
        if (triggersSent.page_load) return;

        const response = await fetch('/Chat/Trigger', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ trigger: 'page_load' })
        });

        const data = await response.json();
        triggersSent.page_load = true;
        handleBotResponse(data);
    }

    // --- TRIGGER: WAIT 5 MINUTES (if no interaction) ---
    let waitTimer = setTimeout(() => {
        if (!hasUserInteracted && !triggersSent.wait_15s) {
            fetch('/Chat/Trigger', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ trigger: 'wait_15s' })
            }).then(r => r.json()).then(data => {
                triggersSent.wait_15s = true;
                handleBotResponse(data);
            });
        }
    }, 300000); // 5 minutes (300,000ms)

    // --- TRIGGER: SCROLL 70% (with debounce to prevent multiple API calls) ---
    let scrollDebounceTimer = null;
    let isScrollTriggerProcessing = false;

    window.addEventListener('scroll', () => {
        // Return early if already triggered or currently processing
        if (triggersSent.scroll_70 || isScrollTriggerProcessing) return;

        // Clear previous debounce timer
        if (scrollDebounceTimer) {
            clearTimeout(scrollDebounceTimer);
        }

        // Debounce: wait 200ms after user stops scrolling
        scrollDebounceTimer = setTimeout(() => {
            const scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
            const scrolled = (window.scrollY / scrollHeight) * 100;

            if (scrolled >= 70 && !triggersSent.scroll_70 && !isScrollTriggerProcessing) {
                isScrollTriggerProcessing = true; // Lock to prevent duplicate calls

                fetch('/Chat/Trigger', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ trigger: 'scroll_70' })
                })
                    .then(r => r.json())
                    .then(data => {
                        triggersSent.scroll_70 = true;
                        handleBotResponse(data);
                    })
                    .catch(error => {
                        console.error('[Chatbot] Scroll trigger error:', error);
                    })
                    .finally(() => {
                        isScrollTriggerProcessing = false; // Unlock after request completes
                    });
            }
        }, 200); // Wait 200ms after scrolling stops
    });

    // --- MARK USER INTERACTION ---
    function markUserInteraction() {
        if (!hasUserInteracted) {
            hasUserInteracted = true;
            clearTimeout(waitTimer); // Cancel wait trigger
        }
    }

    // --- 1. HÀM GỬI TIN NHẮN VĂN BẢN ---
    async function sendMessage(text) {
        if (!text.trim()) return;
        markUserInteraction();
        appendMessage(text, 'user');
        chatInput.value = '';

        try {
            const response = await fetch('/Chat/Send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ text: text.trim() })
            });

            const data = await response.json();
            handleBotResponse(data);
        } catch (error) {
            appendMessage('Xin lỗi, chatbot đang bận.', 'bot');
        }
    }

    // --- 2. HÀM GỬI QUICK REPLY (ĐÃ FIX LỖI KHÔNG KHỚP) ---
    async function sendQuickReply(replyText) {
        // Làm sạch chuỗi trước khi gửi để Python so sánh chính xác 100%
        const cleanReply = replyText.trim();
        markUserInteraction();
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
            appendMessage('Không thể xử lý lựa chọn này.', 'bot');
        }
    }

    // --- 3. HÀM XỬ LÝ PHẢN HỒI TỪ BOT ---
    function handleBotResponse(data) {
        console.log("[Chatbot] Full data received:", data);
        if (!data) return;

        // Backend có thể trả về Success/success, Response/response
        const isSuccess = data.Success !== undefined ? data.Success : data.success;
        const res = data.Response || data.response || data;
        const errorMsg = data.Error || data.error || (res && (res.Error || res.error));

        // Nếu backend báo lỗi
        if (isSuccess === false && errorMsg) {
            appendMessage(`Lỗi hệ thống: ${errorMsg}`, 'bot');
            return;
        }

        // Lấy message: Ưu tiên trong object con (res), sau đó mới ở object cha (data)
        const message = (res && (res.Message || res.message)) || data.Message || data.message;

        if (message) {
            appendMessage(message, 'bot');
        } else if (isSuccess === false) {
            appendMessage("AI Server hiện không có phản hồi hợp lệ.", 'bot');
        } else {
            // Debug: nếu ko tìm thấy message thì hiện JSON để biết nó là gì
            console.warn("[Chatbot] No message found in data", data);
            appendMessage("Mình chưa hiểu ý bạn...", 'bot');
        }

        // Lấy danh sách options
        const options = (res && (res.Options || res.options)) || data.Options || data.options || [];

        if (options.length > 0) {
            const optionsContainer = document.createElement('div');
            optionsContainer.className = 'flex flex-wrap gap-2 mt-2 ml-10 mb-4 animate-fade-in';

            options.forEach(opt => {
                const btn = document.createElement('button');
                btn.className = 'px-4 py-2 bg-blue-50 text-blue-700 rounded-full text-xs font-bold hover:bg-blue-600 hover:text-white transition-all border border-blue-200 shadow-sm active:scale-95';
                btn.innerText = opt;

                btn.onclick = () => {
                    const selectedOption = opt.trim();
                    optionsContainer.remove();
                    sendQuickReply(selectedOption);
                };
                optionsContainer.appendChild(btn);
            });
            chatMessages.appendChild(optionsContainer);
        }

        // --- HIỂN THỊ FORM (type: "form") ---
        const type = (res && (res.Type || res.type)) || data.Type || data.type;
        const formFields = (res && (res.FormFields || res.form_fields)) || data.FormFields || data.form_fields || [];

        if (type === 'form' && formFields.length > 0) {
            renderLeadForm(formFields);
        }

        scrollToBottom();
    }

    // --- 4. HIỂN THỊ TIN NHẮN ---
    function appendMessage(text, sender) {
        if (!text) return;
        const msgWrapper = document.createElement('div');
        msgWrapper.className = sender === 'user' ? 'flex justify-end mb-4' : 'flex gap-3 mb-4';

        const contentClass = sender === 'user'
            ? 'bg-[#2563EB] text-white p-3.5 rounded-2xl rounded-tr-none text-sm shadow-md max-w-[75%] font-medium'
            : 'bg-white text-slate-700 p-3.5 rounded-2xl rounded-tl-none shadow-md text-sm max-w-[75%] border border-gray-100 font-medium';

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

    // --- 5. RENDER FORM THU THẬP THÔNG TIN LEAD ---
    function renderLeadForm(fields) {
        const formWrapper = document.createElement('div');
        formWrapper.className = 'flex gap-3 mb-4 animate-fade-in';

        const avatarHtml = `<div class="w-9 h-9 rounded-full bg-gradient-to-tr from-blue-500 to-indigo-600 flex items-center justify-center text-white shadow-lg flex-shrink-0"><i class="fa-solid fa-robot text-sm"></i></div>`;

        // Xây dựng các input dựa trên tên field từ Python
        const inputsHtml = fields.map((fieldLabel, idx) => {
            const isRequired = fieldLabel.toLowerCase().includes('bắt buộc');
            const isEmail = fieldLabel.toLowerCase().includes('email');
            const isPhone = fieldLabel.toLowerCase().includes('điện thoại') || fieldLabel.toLowerCase().includes('phone');
            const inputType = isEmail ? 'email' : isPhone ? 'tel' : 'text';
            const placeholder = fieldLabel.replace(/\s*\(.*?\)/g, '').trim();
            return `
                    <div style="margin-bottom:8px;">
                        <label style="display:block;font-size:11px;font-weight:600;color:#475569;margin-bottom:3px;">
                            ${placeholder}${isRequired ? ' <span style="color:#ef4444;">*</span>' : ''}
                        </label>
                        <input
                            type="${inputType}"
                            data-field="field_${idx}"
                            placeholder="${placeholder}"
                            ${isRequired ? 'required' : ''}
                            style="width:100%;padding:7px 10px;border:1.5px solid #cbd5e1;border-radius:8px;font-size:12px;outline:none;transition:border-color 0.2s;"
                            onfocus="this.style.borderColor='#2563EB';"
                            onblur="this.style.borderColor='#cbd5e1';"
                        />
                    </div>`;
        }).join('');

        const formHtml = `
                <form id="chatbot-lead-form" style="background:#fff;border:1.5px solid #e2e8f0;border-radius:14px;padding:14px 16px;box-shadow:0 2px 8px rgba(0,0,0,0.07);min-width:220px;max-width:280px;">
                    ${inputsHtml}
                    <button
                        type="submit"
                        style="width:100%;margin-top:4px;padding:8px;background:linear-gradient(135deg,#2563EB,#4f46e5);color:#fff;font-size:12px;font-weight:700;border:none;border-radius:8px;cursor:pointer;transition:opacity 0.2s;"
                        onmouseover="this.style.opacity='0.88';"
                        onmouseout="this.style.opacity='1';"
                    >Gửi thông tin</button>
                    <p id="chatbot-form-msg" style="margin-top:6px;font-size:11px;color:#16a34a;display:none;text-align:center;"></p>
                </form>`;

        formWrapper.innerHTML = `${avatarHtml}<div>${formHtml}</div>`;
        chatMessages.appendChild(formWrapper);
        scrollToBottom();

        // Xử lý submit form - chỉ cảm ơn, không gọi API
        const form = formWrapper.querySelector('#chatbot-lead-form');
        form.addEventListener('submit', (e) => {
            e.preventDefault();
            const inputs = form.querySelectorAll('input');
            let valid = true;

            inputs.forEach(input => {
                if (input.required && !input.value.trim()) {
                    input.style.borderColor = '#ef4444';
                    valid = false;
                } else {
                    input.style.borderColor = '#cbd5e1';
                }
            });

            if (!valid) return;

            // Ẩn form, hiện cảm ơn
            formWrapper.remove();
            appendMessage('Cảm ơn anh/chị! 🎉 Đội ngũ ToánHọcHay sẽ liên hệ để hướng dẫn học thử sớm nhất.', 'bot');
        });
    }

    if (chatSendBtn) chatSendBtn.addEventListener('click', () => sendMessage(chatInput.value));
    if (chatInput) chatInput.addEventListener('keypress', (e) => { if (e.key === 'Enter') sendMessage(chatInput.value); });

    // --- TRIGGER PAGE LOAD KHI LOAD XONG ---
    triggerPageLoad();
});