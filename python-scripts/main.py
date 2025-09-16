from fastapi import FastAPI, HTTPException, Form
from fastapi.responses import JSONResponse
import os
from datetime import datetime
import threading
from pathlib import Path
import csv
from typing import Optional, Union

CSV_BASE_PATH = "data"
DEFAULT_CSV_FILE_NAME = "button-press-timestamp"

# Thread lock for file operations
file_lock = threading.Lock()


def ensure_data_directory():
    """Ensure data directory exists"""
    Path(CSV_BASE_PATH).mkdir(parents=True, exist_ok=True)


def write_to_csv(filepath: str, rows: Union[list, str], mode: str = "a"):
    """Write multiple rows to CSV file at once"""

    with open(filepath, mode, newline="", encoding="utf-8") as file:
        writer = csv.writer(file)

        if mode == "w":
            writer.writerow(rows)
        else:
            writer.writerows(rows)


def write_batch_to_csv(file_path: str, csv_rows: list, headers: Optional[list]):
    """Write batch data to CSV file"""
    with file_lock:
        if headers and not os.path.exists(file_path):
            write_to_csv(file_path, headers, mode="w")

        write_to_csv(file_path, csv_rows)


app = FastAPI(title="Arduino CSV Data Logger", version="1.0.0")


@app.post("/export")
def export_arduino_data(data: str = Form(...), headers: Optional[str] = Form(None)):
    """Export batch CSV data to persistent file"""
    try:
        now = datetime.now()
        file_name = f"{DEFAULT_CSV_FILE_NAME}-{now.strftime("%Y-%m-%d_%H-%M-%S")}.csv"
        file_path = os.path.join(CSV_BASE_PATH, file_name)

        headers_list = [header.strip() for header in headers.split(",")]

        # Split batch into individual rows and parse
        csv_rows = []
        raw_rows = data.split("\\n")  # Split on escaped newlines

        for row_string in raw_rows:
            row_string = row_string.strip()
            if row_string:  # Skip empty rows
                row_data = [item.strip() for item in row_string.split(",")]
                csv_rows.append(row_data)

        ensure_data_directory()
        write_batch_to_csv(file_path, csv_rows, headers_list)

        return JSONResponse(
            content={
                "status": "success",
                "message": f"CSV data exported to {file_name}",
                "timestamp": datetime.now().isoformat(),
            },
            status_code=200,
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error exporting CSV: {str(e)}")


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app)
