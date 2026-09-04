document.addEventListener("DOMContentLoaded", async () => {
    await loadChapters();
});

async function loadChapters() {
    const res = await fetch("/api/chapters/select");
    const data = await res.json();

    const select = document.getElementById("chapterSelect");
    data.forEach(c => {
        select.innerHTML += `<option value="${c.id}">${c.name}</option>`;
    });

    select.innerHTML += `<option value="new">+ Tạo mới</option>`;
}

document.getElementById("chapterSelect").onchange = async function () {
    const value = this.value;

    if (value === "new") return;

    const res = await fetch(`/api/topics/by-chapter/${value}`);
    const topics = await res.json();

    const topicSelect = document.getElementById("topicSelect");
    topicSelect.innerHTML = '<option value="">-- Chọn topic --</option>';

    topics.forEach(t => {
        topicSelect.innerHTML += `<option value="${t.id}">${t.name}</option>`;
    });

    topicSelect.innerHTML += `<option value="new">+ Tạo mới</option>`;
};

document.getElementById("btnSave").onclick = async () => {

    const payload = {
        chapter: null,
        topic: null,
        lesson: null,
        lessonContents: []
    };

    const chapterValue = chapterSelect.value;
    if (chapterValue)
        payload.chapter = { chapterId: parseInt(chapterValue) };

    const topicValue = topicSelect.value;
    if (topicValue)
        payload.topic = { topicId: parseInt(topicValue) };

    payload.lesson = {
        lessonName: document.getElementById("lessonName").value,
        createdBy: parseInt(document.getElementById("createdBy").value)
    };

    payload.lessonContents.push({
        blockType: 1,
        contentText: "Test content",
        orderIndex: 1
    });

    console.log(payload);

    await fetch("/api/lesson-data/create-or-add", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    });

    if (window.THHToast) THHToast.success("Đã lưu!");
    else alert("Đã lưu!");
};
