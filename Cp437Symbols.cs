using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace askiifarm
{
    public static class Cp437Symbols
    {
        // --- Лица, масти и знаки ---
        public const char WhiteSmilingFace = '\u263A';      // ☺
        public const char BlackSmilingFace = '\u263B';      // ☻
        public const char BlackHeartSuit = '\u2665';        // ♥
        public const char BlackDiamondSuit = '\u2666';      // ♦
        public const char BlackClubSuit = '\u2663';         // ♣
        public const char BlackSpadeSuit = '\u2660';        // ♠
        public const char Bullet = '\u2022';                // •
        public const char InverseBullet = '\u25D8';         // ◘
        public const char WhiteCircle = '\u25CB';           // ○
        public const char InverseWhiteCircle = '\u25D9';    // ◙
        public const char MaleSign = '\u2642';              // ♂
        public const char FemaleSign = '\u2640';            // ♀
        public const char EighthNote = '\u266A';            // ♪
        public const char BeamedEighthNotes = '\u266B';     // ♫
        public const char WhiteSunWithRays = '\u263C';      // ☼
        public const char PilcrowSign = '\u00B6';           // ¶
        public const char SectionSign = '\u00A7';           // §
        public const char House = '\u2302';                 // ⌂

        // --- Стрелки и указатели ---
        public const char BlackRightPointingPointer = '\u25BA'; // ►
        public const char BlackLeftPointingPointer = '\u25C4';  // ◄
        public const char UpDownArrow = '\u2195';               // ↕
        public const char UpDownArrowWithBase = '\u21A8';       // ↨
        public const char UpwardsArrow = '\u2191';              // ↑
        public const char DownwardsArrow = '\u2193';            // ↓
        public const char RightwardsArrow = '\u2192';           // →
        public const char LeftwardsArrow = '\u2190';            // ←
        public const char LeftRightArrow = '\u2194';            // ↔
        public const char BlackUpPointingTriangle = '\u25B2';   // ▲
        public const char BlackDownPointingTriangle = '\u25BC'; // ▼

        // --- Блоки и градиенты ---
        public const char LightShade = '\u2591';        // ░
        public const char MediumShade = '\u2592';       // ▒
        public const char DarkShade = '\u2593';         // ▓
        public const char FullBlock = '\u2588';         // █
        public const char LowerHalfBlock = '\u2584';    // ▄
        public const char LeftHalfBlock = '\u258C';     // ▌
        public const char RightHalfBlock = '\u2590';    // ▐
        public const char UpperHalfBlock = '\u2580';    // ▀
        public const char BlackSquare = '\u25A0';       // ■
        public const char BlackRectangle = '\u25AC';    // ▬

        // --- Одинарные рамки (Box Drawings Light) ---
        public const char BoxDrawingsLightHorizontal = '\u2500';          // ─
        public const char BoxDrawingsLightVertical = '\u2502';            // │
        public const char BoxDrawingsLightDownAndRight = '\u250C';        // ┌
        public const char BoxDrawingsLightDownAndLeft = '\u2510';         // ┐
        public const char BoxDrawingsLightUpAndRight = '\u2514';          // └
        public const char BoxDrawingsLightUpAndLeft = '\u2518';           // ┘
        public const char BoxDrawingsLightVerticalAndRight = '\u251C';    // ├
        public const char BoxDrawingsLightVerticalAndLeft = '\u2524';     // ┤
        public const char BoxDrawingsLightDownAndHorizontal = '\u252C';   // ┬
        public const char BoxDrawingsLightUpAndHorizontal = '\u2534';     // ┴
        public const char BoxDrawingsLightVerticalAndHorizontal = '\u253C'; // ┼

        // --- Двойные рамки (Box Drawings Double) ---
        public const char BoxDrawingsDoubleHorizontal = '\u2550';          // ═
        public const char BoxDrawingsDoubleVertical = '\u2551';            // ║
        public const char BoxDrawingsDoubleDownAndRight = '\u2554';        // ╔
        public const char BoxDrawingsDoubleDownAndLeft = '\u2557';         // ╗
        public const char BoxDrawingsDoubleUpAndRight = '\u255A';          // ╚
        public const char BoxDrawingsDoubleUpAndLeft = '\u255D';           // ╝
        public const char BoxDrawingsDoubleVerticalAndRight = '\u2560';    // ╠
        public const char BoxDrawingsDoubleVerticalAndLeft = '\u2563';     // ╣
        public const char BoxDrawingsDoubleDownAndHorizontal = '\u2566';   // ╦
        public const char BoxDrawingsDoubleUpAndHorizontal = '\u2569';     // ╩
        public const char BoxDrawingsDoubleVerticalAndHorizontal = '\u256C'; // ╬

        // --- Комбинированные рамки (Single & Double) ---
        public const char BoxDrawingsDownSingleAndRightDouble = '\u2552'; // ╒
        public const char BoxDrawingsDownDoubleAndRightSingle = '\u2553'; // ╓
        public const char BoxDrawingsDownSingleAndLeftDouble = '\u2555';  // ╕
        public const char BoxDrawingsDownDoubleAndLeftSingle = '\u2556';  // ╖
        public const char BoxDrawingsUpSingleAndRightDouble = '\u2558';   // ╘
        public const char BoxDrawingsUpDoubleAndRightSingle = '\u2559';   // ╙
        public const char BoxDrawingsUpSingleAndLeftDouble = '\u255B';    // ╛
        public const char BoxDrawingsUpDoubleAndLeftSingle = '\u255C';    // ╜
        public const char BoxDrawingsVerticalSingleAndRightDouble = '\u255E'; // ╞
        public const char BoxDrawingsVerticalDoubleAndRightSingle = '\u255F'; // ╟
        public const char BoxDrawingsVerticalSingleAndLeftDouble = '\u2561';  // ╡
        public const char BoxDrawingsVerticalDoubleAndLeftSingle = '\u2562';  // ╢
        public const char BoxDrawingsDownSingleAndHorizontalDouble = '\u2564'; // ╤
        public const char BoxDrawingsDownDoubleAndHorizontalSingle = '\u2565'; // ╥
        public const char BoxDrawingsUpSingleAndHorizontalDouble = '\u2567';   // ╧
        public const char BoxDrawingsUpDoubleAndHorizontalSingle = '\u2568';   // ╨
        public const char BoxDrawingsVerticalSingleAndHorizontalDouble = '\u256A'; // ╪
        public const char BoxDrawingsVerticalDoubleAndHorizontalSingle = '\u256B'; // ╫

        // --- Математические и прочие символы ---
        public const char DoubleExclamationMark = '\u203C';    // ‼
        public const char RightAngle = '\u221F';               // ∟
        public const char IdenticalTo = '\u2261';              // ≡
        public const char PlusMinusSign = '\u00B1';            // ±
        public const char GreaterThanOrEqualTo = '\u2265';     // ≥
        public const char LessThanOrEqualTo = '\u2264';        // ≤
        public const char DivisionSign = '\u00F7';             // ÷
        public const char AlmostEqualTo = '\u2248';            // ≈
        public const char DegreeSign = '\u00B0';               // °
        public const char BulletOperator = '\u2219';           // ∙
        public const char MiddleDot = '\u00B7';                // ·
        public const char SquareRoot = '\u221A';               // √
    }
}
