using ToDoListApi.Models;
using Xunit;

namespace ToDoListApi.Tests
{
    public class TodoItemTests
    {
        [Fact]
        public void Rename_GecerliBaslikVerilince_BasligiGuncellemeli()
        {
            // Arrange (hazırla)
            var todo = new TodoItem("Eski başlık", 1);

            // Act (çalıştır)
            todo.Rename("Yeni başlık");

            // Assert (doğrula)
            Assert.Equal("Yeni başlık", todo.Title);
        }
        
        [Fact]
        public void Rename_BosBaslikVerilince_HataFirlatmali()
        {
            // Arrange
            var todo = new TodoItem("Eski başlık", 1);

             // Act & Assert
            Assert.Throws<ArgumentException>(() => todo.Rename(""));
        }
    }
}
