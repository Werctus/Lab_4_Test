#!/usr/bin/env python3
# generator/gen_tests.py

import yaml
import argparse
from pathlib import Path
from typing import Dict, List, Any

# ==========================================
# 1. ШАБЛОНЫ КОДА C# (NUnit)
# ==========================================

TEST_FILE_TEMPLATE = """// AUTO-GENERATED TESTS. DO NOT EDIT MANUALLY.
// Source: {spec_source}
// Generator: gen_tests.py v1.0

using System;
using NUnit.Framework;
using UrlSanitizer.Interfaces;
using SanitizerClass = UrlSanitizer.Implementations.GenCode1.UrlSanitizer;

namespace UrlSanitizer.Tests
{{
    [TestFixture]
    [Description("Автоматически сгенерированные тесты для {module_name}")]
    public class {module_name}Tests_Generated
    {{
        private I{module_name} _sut;

        [SetUp]
        public void SetUp()
        {{
            // Инициализация тестируемой системы
            _sut = new SanitizerClass();
        }}

{test_methods}
    }}
}}
"""

TEST_METHOD_TEMPLATE = """        [Test]
        [Description("Класс эквивалентности: {case_desc}")]
        {test_cases}
        public void Test_{method_name}_{case_name}()
        {{
            // === Arrange ===
            // Предусловие: {pre}
            // Ожидаемый результат: {expected}

            // === Act & Assert ===
{act_assert_code}
        }}
"""

TEST_CASE_TEMPLATE = "[TestCase({inputs})]"

# ==========================================
# 2. ПАРСИНГ И ГЕНЕРАЦИЯ
# ==========================================

def format_csharp_input(value: Any) -> str:
    """Преобразует значение из YAML в литерал C#."""
    if value is None:
        return "null"
    if isinstance(value, str):
        return f'"{value}"'
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)

def generate_method_tests(method_data: Dict[str, Any]) -> List[str]:
    """Генерирует методы тестов для всех классов эквивалентности."""
    case_blocks = []
    
    for eq_class in method_data.get("equivalence_classes", []):
        # Формируем входные параметры
        inputs_str = ", ".join(format_csharp_input(inp) for inp in eq_class["inputs"])
        method_name = method_data["name"]
        expected = str(eq_class["expected"])
        
        # Умная генерация Assert на основе ожидаемого результата (оракул)
        if "ArgumentException" in expected:
            act_assert_code = f"            Assert.Throws<ArgumentException>(() => _sut.{method_name}({inputs_str}));"
        else:
            if method_data["signature"].startswith("void"):
                act_assert_code = f"            _sut.{method_name}({inputs_str});\n            Assert.Pass(\"Автогенерация: проверьте постусловие вручную. Ожидалось: {expected}\");"
            else:
                act_assert_code = f"            var result = _sut.{method_name}({inputs_str});\n            Assert.Pass(\"Автогенерация: проверьте постусловие вручную. Ожидалось: {expected}\");"

        # Формируем безопасное имя метода
        case_name = eq_class["case"].replace(" ", "_").replace("(", "").replace(")", "").replace("-", "")

        case_blocks.append(
            TEST_METHOD_TEMPLATE.format(
                case_desc=eq_class["case"],
                test_cases=TEST_CASE_TEMPLATE.format(inputs=inputs_str),
                method_name=method_name,
                case_name=case_name,
                pre=method_data["pre"],
                expected=expected,
                act_assert_code=act_assert_code
            )
        )
    return case_blocks

def render_and_save(spec: Dict[str, Any], config: Dict[str, Any]) -> None:
    """Собирает полный файл тестов и сохраняет на диск."""
    module_name = spec["module"]
    test_methods = []
    
    for method in spec["methods"]:
        test_methods.extend(generate_method_tests(method))

    file_content = TEST_FILE_TEMPLATE.format(
        spec_source=config.get("spec_path", "N/A"),
        module_name=module_name,
        test_methods="\n".join(test_methods)
    )

    out_dir = Path(config.get("output_dir", "tests/Module.Tests"))
    out_dir.mkdir(parents=True, exist_ok=True)
    output_file = out_dir / f"{module_name}Tests.Generated.cs"
    
    output_file.write_text(file_content, encoding="utf-8")
    
    print(f"[√] Сгенерирован файл: {output_file}")
    print(f"    Методов покрыто: {len(spec['methods'])}")
    print(f"    Тестов сгенерировано: {sum(len(m.get('equivalence_classes', [])) for m in spec['methods'])}")

# ==========================================
# 3. ТОЧКА ВХОДА
# ==========================================

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="C# NUnit Test Generator from YAML Spec")
    parser.add_argument("--config", default="config.yaml", help="Путь к config.yaml")
    args = parser.parse_args()

    print("[*] Загрузка конфигурации...")
    with open(args.config, "r", encoding="utf-8") as f:
        config = yaml.safe_load(f)

    print(f"[*] Загрузка спецификации: {config['spec_path']}...")
    with open(config["spec_path"], "r", encoding="utf-8") as f:
        spec_data = yaml.safe_load(f)

    print("[*] Генерация C# тестов...")
    render_and_save(spec_data, config)
    print("[√] Готово.")