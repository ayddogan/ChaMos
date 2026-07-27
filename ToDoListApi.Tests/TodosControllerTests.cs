using Microsoft.AspNetCore.Mvc;
using Moq;
using ToDoListApi.Controllers;
using ToDoListApi.Models;
using ToDoListApi.Repositories;
using Xunit;

namespace ToDoListApi.Tests
{
    public class TodosControllerTests
    {
        [Fact]
        public async Task GetTodo_KayitVarsa_OkIleBirlikteGoreviDondurmeli()
        {
            // Arrange
            var mockRepo = new Mock<ITodoRepository>();
            var beklenenTodo = new TodoItem("Test görevi", 1);

            mockRepo
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(beklenenTodo);

            var controller = new TodosController(mockRepo.Object);

            // Act
            var sonuc = await controller.GetTodo(1);

            // Assert
            var okResult = Assert.IsType<ActionResult<TodoItem>>(sonuc);
            Assert.Equal(beklenenTodo, okResult.Value);
        }

        [Fact]
        public async Task GetTodo_KayitYoksa_NotFoundDondurmeli()
        {
            // Arrange
            var mockRepo = new Mock<ITodoRepository>();

            mockRepo
                .Setup(repo => repo.GetByIdAsync(999))
                .ReturnsAsync((TodoItem?)null);

            var controller = new TodosController(mockRepo.Object);

            // Act
            var sonuc = await controller.GetTodo(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(sonuc.Result);
        }
    }
}