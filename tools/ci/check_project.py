#!/usr/bin/env python3
"""Быстрая проверка репозитория без Unity (CI, этап «checks»).

1. У каждого файла и папки в Assets/ есть .meta, и у каждого .meta есть ассет.
2. Данные игры (Assets/Resources/Data) ссылаются только на ассеты, которые есть в репозитории.
   Именно это ломается, когда в коммит не попали новые файлы (например, префабы атак).

Запуск из корня проекта:  python3 tools/ci/check_project.py
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSETS = os.path.join(ROOT, "Assets")
DATA = os.path.join(ASSETS, "Resources", "Data")
GUID_REF = re.compile(r"guid: ([0-9a-f]{32})")
BUILTIN = {"0000000000000000e000000000000000", "0000000000000000f000000000000000"}

errors = []
known_guids = set()

for dirpath, dirnames, filenames in os.walk(ASSETS):
    dirnames[:] = [d for d in dirnames if not d.startswith(".")]
    entries = [os.path.join(dirpath, d) for d in dirnames] + [os.path.join(dirpath, f) for f in filenames]
    for path in entries:
        name = os.path.basename(path)
        rel = os.path.relpath(path, ROOT)
        if name.startswith("."):
            continue
        if name.endswith(".meta"):
            target = path[:-5]
            if not os.path.exists(target):
                errors.append(f".meta без ассета: {rel}")
            with open(path, encoding="utf-8", errors="replace") as f:
                m = re.search(r"^guid: ([0-9a-f]{32})", f.read(), re.M)
                if m:
                    known_guids.add(m.group(1))
        elif not os.path.exists(path + ".meta"):
            errors.append(f"нет .meta: {rel}  (добавьте его в коммит вместе с ассетом)")

for dirpath, _, filenames in os.walk(DATA):
    for f in filenames:
        if f.endswith(".meta"):
            continue
        path = os.path.join(dirpath, f)
        with open(path, encoding="utf-8", errors="replace") as fh:
            for guid in set(GUID_REF.findall(fh.read())):
                if guid not in known_guids and guid not in BUILTIN:
                    errors.append(f"{os.path.relpath(path, ROOT)} ссылается на guid {guid}, которого нет в репозитории "
                                  f"(забытый файл в коммите?)")

if errors:
    print("Проверка проекта: найдены проблемы\n")
    for e in errors:
        print("  ✗ " + e)
    sys.exit(1)
print(f"Проверка проекта: всё в порядке ({len(known_guids)} ассетов)")
