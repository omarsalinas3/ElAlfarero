"""
Exporta los documentos de entrega (GDD y manual de usuario) de Markdown a PDF (#39).

Convierte cada .md a HTML con estilo de impresión y lo imprime con Edge o Chrome sin interfaz (--headless),
así no hace falta instalar LaTeX ni Pandoc. Requiere:  pip install markdown

Uso (desde la raíz del repositorio):  python Docs/exportar_pdf.py
"""
import os
import subprocess
import sys
import tempfile

import markdown

DOCS = os.path.dirname(os.path.abspath(__file__))
DOCUMENTOS = [
    ("GDD.md", "GDD_El_Alfarero_De_Dolores_V3.pdf"),
    ("MANUAL_USUARIO.md", "Manual_de_usuario_El_Alfarero_De_Dolores.pdf"),
]
NAVEGADORES = [
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
]

ESTILO = """
@page { size: Letter; margin: 18mm 16mm; }
body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 10.5pt; line-height: 1.45; color: #2b1d14; }
h1 { color: #7a2e12; border-bottom: 3px solid #c9772f; padding-bottom: 4px; font-size: 20pt; }
h2 { color: #7a2e12; border-bottom: 1px solid #e3c9a8; padding-bottom: 2px; margin-top: 22px; page-break-after: avoid; }
h3, h4 { color: #2f4f9a; page-break-after: avoid; }
table { border-collapse: collapse; width: 100%; margin: 8px 0 12px; font-size: 9.5pt; page-break-inside: avoid; }
th { background: #f3e2cc; color: #4a2a14; }
th, td { border: 1px solid #d9bfa0; padding: 4px 6px; vertical-align: top; text-align: left; }
tr:nth-child(even) td { background: #fbf5ee; }
code { background: #f3ece4; padding: 0 3px; border-radius: 3px; font-size: 9pt; }
blockquote { border-left: 4px solid #c9772f; margin: 8px 0; padding: 2px 10px; color: #5a4636; background: #fbf5ee; }
hr { border: 0; border-top: 1px solid #e3c9a8; }
a { color: #2f4f9a; text-decoration: none; }
"""


def navegador():
    for ruta in NAVEGADORES:
        if os.path.exists(ruta):
            return ruta
    sys.exit("No se encontró Edge ni Chrome para imprimir el PDF.")


def exportar(md, pdf, exe):
    with open(os.path.join(DOCS, md), encoding="utf-8") as f:
        cuerpo = markdown.markdown(f.read(), extensions=["tables", "fenced_code", "sane_lists"])
    html = f"<!doctype html><html lang='es'><head><meta charset='utf-8'><title>{md}</title><style>{ESTILO}</style></head><body>{cuerpo}</body></html>"
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False, encoding="utf-8") as tmp:
        tmp.write(html)
    salida = os.path.join(DOCS, pdf)
    subprocess.run([exe, "--headless", "--disable-gpu", "--no-pdf-header-footer",
                    f"--print-to-pdf={salida}", "file:///" + tmp.name.replace("\\", "/")],
                   check=True, capture_output=True, timeout=120)
    os.remove(tmp.name)
    print(f"{md} -> {pdf}")


if __name__ == "__main__":
    exe = navegador()
    for md, pdf in DOCUMENTOS:
        exportar(md, pdf, exe)
