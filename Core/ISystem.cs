// Core/ISystem.cs
namespace AsciiFarm.Core;

/// <summary>Обработчик. Всё поведение игры — здесь, а не в данных.</summary>
public interface ISystem
{
    void Update(World world);
}