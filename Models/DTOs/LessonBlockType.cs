namespace ToanHocHay.WebApp.Models.DTOs
{
    // Tên phải khớp enum LessonBlockType của backend (serialize dạng chuỗi).
    public enum LessonBlockType
    {
        Heading, Text, Definition, Example, Note, Formula,
        Image, Video, Animation, Embed, Audio, Pdf
    }
}