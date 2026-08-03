import zipfile, re, os
files = [
    'Monk_Fruit_Sweetener_Website_Blueprint.docx',
    'Monk_Fruit_Sweetener_Technical_Development_Document.docx'
]
for name in files:
    path = os.path.join(os.getcwd(), name)
    print('===== ' + name + ' =====')
    with zipfile.ZipFile(path) as z:
        xml = z.read('word/document.xml').decode('utf-8', errors='ignore')
    text = re.sub(r'<[^>]+>', ' ', xml)
    text = re.sub(r'\s+', ' ', text)
    text = text.replace('&nbsp;', ' ')
    print(text[:12000])
    print('\n')
