from pathlib import Path
import json
import xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[1]
keys = ["Loading application…", "The application encountered an error.", "Reload", "Dismiss error", "Checking session…", "Rejoining the server…", "Retry in", "seconds.", "Failed to rejoin. Please retry or reload the page.", "Retry", "The session has been paused by the server.", "Failed to resume the session. Please retry or reload the page.", "Resume", "Error", "An error occurred while processing your request.", "Request ID", "Development Mode", "Use the Development environment for detailed errors during local debugging only.", "Do not enable Development on deployed applications: exception details can expose sensitive information."]
result = {}
for language, suffix in [("en-US", ""), ("de-DE", ".de"), ("ru-RU", ".ru")]:
    source = ET.parse(root / f"CorporateStarter.Shared/Localization/Strings{suffix}.resx")
    values = {row.attrib["name"]: row.findtext("value") for row in source.getroot().findall("data")}
    result[language] = {key: values["term." + key if key[0].islower() else key] for key in keys}
(root / "CorporateStarter.Client.UI/wwwroot/localization-boot.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
