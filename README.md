# 🏷️ RSRBarcode

![.NET](https://img.shields.io/badge/.NET-10.0--windows-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=csharp&logoColor=white)
![ZXing](https://img.shields.io/badge/ZXing-SkiaSharp-1f6feb?style=flat-square)
![PDFtoImage](https://img.shields.io/badge/PDFtoImage-5.2-1f6feb?style=flat-square)
![Blog](https://img.shields.io/badge/blog-rsrsystem-FF5722?style=flat-square&logo=blogger&logoColor=white)

> Librería **.NET 10** para **generar y leer códigos de barras** (QR, CODE-39), incluso **leyéndolos directamente de un PDF**.

## 📋 ¿Qué es esto?

El "todo en uno" de códigos de barras para PowerBuilder: **genera** QR y CODE-39, **lee** códigos de
una imagen y, lo más cómodo, **lee un código directamente desde un PDF** (rasteriza la página y
decodifica).

```csharp
public class RSRbarcode
{
    string ReadBarcodePDF(string inputFile);                 // PDF → rasteriza → lee
    string ReadBarcode(string imageName);                    // imagen → lee
    void   BarcodeGenerate(string source, string outputFile);// CODE-39 → PNG
    void   QrGenerate(string source, string outputFile);     // QR → PNG
}
```

## 🧩 Dependencias

| Paquete | Versión |
|---------|---------|
| [PDFtoImage](https://www.nuget.org/packages/PDFtoImage) | `5.2.1` |
| [ZXing.Net](https://www.nuget.org/packages/ZXing.Net) | `0.16.11` |
| [ZXing.Net.Bindings.SkiaSharp](https://www.nuget.org/packages/ZXing.Net.Bindings.SkiaSharp) | `0.16.22` |
| [System.Drawing.Common](https://www.nuget.org/packages/System.Drawing.Common) | `10.0.9` |

> 🆕 **Migración a .NET 10:** se sustituyó el **abandonado PdfiumViewer (2018)** por **PDFtoImage**
> (MIT, PDFium + SkiaSharp) para rasterizar el PDF, y la lectura/generación pasó al binding
> **ZXing SkiaSharp** (en lugar de `ZXing.Windows.Compatibility`).

## 🛠️ Requisitos

- **.NET SDK 10.0** o superior
- **Windows** (`net10.0-windows`; System.Drawing se usa solo para re-codificar a BMP)

## 🚀 Compilar

```bat
dotnet build RSRBarcode.csproj -c Release
```

## 👤 Autor

**Ramón San Félix Ramón** — © 2023
🔗 [github.com/rasanfe](https://github.com/rasanfe)

---

📨 **Blog:** <https://rsrsystem.blogspot.com/>

> ¡Nos vemos en el próximo artículo! Y recuerda: en PowerBuilder, los límites solo están en nuestra imaginación. 🚀
