using System.Collections.Generic;
using UnityEngine;

/// <summary>Varian bangunan Desa Pemula (ukuran selaras dengan V2.Size di Vil2Plan.cs).</summary>
public static partial class V2Kit
{
    public static float ThatchA(GeoBag b, int v)
    {
        var s = new HSpec { w = 6f, d = 5.2f, floors = 1, wallH = 2.5f, rise = 2.5f, wall = v % 2 == 0 ? 0 : 3, roof = 0, seed = 11 + v * 5, windowsFront = 2, windowsSide = 1, chimney = v % 2, doorX = v % 2 == 0 ? 0.6f : -0.6f, porch = v == 1,
            eyebrows = v == 0 ? 1 : 0, ivy = v == 0, archDoor = v == 0, archWin = v == 1 };
        return House(b, s);
    }

    public static float ThatchB(GeoBag b, int v)
    {
        var s = new HSpec { w = 8f, d = 5.4f, floors = 1, wallH = 2.5f, rise = 2.6f, wall = v % 2 == 0 ? 0 : 2, roof = 0, seed = 31 + v * 3, windowsFront = 3, windowsSide = 1, chimney = 1, doorX = -1f, porch = v == 0,
            eyebrows = 2, archDoor = true, archWin = v == 1, ivy = v == 1 };
        return House(b, s);
    }

    public static float Cabin(GeoBag b, int v)
    {
        var s = new HSpec { w = 6.4f, d = 5.4f, floors = 1, wallH = 2.6f, rise = 2.1f, wall = 1, roof = 1, seed = 51 + v * 7, windowsFront = 2, windowsSide = 1, chimney = 1, doorX = 0f, ridgeAlongZ = v == 1, porch = v == 0, shutters = true,
            hood = v == 1, ivy = v == 1, archDoor = false };
        return House(b, s);
    }

    public static float Farm(GeoBag b, int v)
    {
        var s = new HSpec { w = 8.6f, d = 6.6f, floors = 2, wallH = 2.7f, rise = 3.3f, wall = 2, roof = 1, seed = 71 + v * 9, windowsFront = 3, windowsSide = 1, chimney = 1, doorX = 0f, porch = true, jetty = false,
            dormers = 2, archWin = true, archDoor = true, ivy = true };
        return House(b, s);
    }

    public static float Tudor(GeoBag b, int v)
    {
        var s = new HSpec { w = 7.6f, d = 6.2f, floors = 2, wallH = 2.6f, rise = 3.0f, wall = 0, roof = 1, seed = 91 + v * 11, windowsFront = 3, windowsSide = 1, chimney = 0, doorX = v == 0 ? -1.2f : 1.2f, jetty = true,
            dormers = 1, hood = true };
        return House(b, s);
    }

    // ------------------------------------------------------------------ preview
    public static void P_ThatchA0(GeoBag b) { ThatchA(b, 0); }
    public static void P_ThatchA1(GeoBag b) { ThatchA(b, 1); }
    public static void P_ThatchB0(GeoBag b) { ThatchB(b, 0); }
    public static void P_Cabin0(GeoBag b) { Cabin(b, 0); }
    public static void P_Cabin1(GeoBag b) { Cabin(b, 1); }
    public static void P_Farm0(GeoBag b) { Farm(b, 0); }
    public static void P_Tudor0(GeoBag b) { Tudor(b, 0); }
}
