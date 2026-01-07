import json
import threading
from pathlib import Path
from dataclasses import dataclass

import tkinter as tk
from tkinter import ttk, messagebox
import requests

CONFIG = Path("config.json")
API_BASE = "https://api.switch-bot.com/v1.0"


@dataclass(frozen=True)
class Device:
    name: str
    ble_mac: str


def load_config(path: Path) -> tuple[str, list[Device]]:
    if not path.exists():
        raise FileNotFoundError(f"Config not found: {path.resolve()}")

    cfg = json.loads(path.read_text(encoding="utf-8"))
    api_token = (cfg.get("api_token") or "").strip()
    if not api_token:
        raise ValueError("api_token is empty in config.json")

    raw = cfg.get("devices")
    if not isinstance(raw, list) or not raw:
        raise ValueError("devices must be a non-empty list")

    devices: list[Device] = []
    for d in raw:
        if not isinstance(d, dict):
            continue
        name = (d.get("name") or "").strip()
        did = (d.get("ble_mac") or "").strip()
        if name and did:
            devices.append(Device(name, did))

    if not devices:
        raise ValueError("No valid devices in config.json")
    return api_token, devices


def post_command(api_token: str, ble_mac: str, command: str) -> requests.Response:
    url = f"{API_BASE}/devices/{ble_mac}/commands"
    headers = {"Authorization": api_token, "Content-Type": "application/json; charset=utf8"}
    payload = {"command": command, "parameter": "default", "commandType": "command"}
    return requests.post(url, headers=headers, data=json.dumps(payload), timeout=10)


class App(ttk.Frame):
    def __init__(self, master: tk.Tk):
        super().__init__(master, padding=10)
        self.master = master
        self.api_token = ""
        self.devices: list[Device] = []
        self.status = tk.StringVar(value="")

        self._build()
        self._load()
        self._render()

    # ---- UI ----
    def _build(self):
        self.master.title("SwitchBot Controller")
        self.master.geometry("340x340")

        self.grid(sticky="nsew")
        self.master.rowconfigure(0, weight=1)
        self.master.columnconfigure(0, weight=1)
        self.rowconfigure(0, weight=1)
        self.columnconfigure(0, weight=1)

        # Scrollable list area (auto-hide scrollbar)
        wrap = ttk.Frame(self)
        wrap.grid(row=0, column=0, sticky="nsew")
        wrap.rowconfigure(0, weight=1)
        wrap.columnconfigure(0, weight=1)

        self.canvas = tk.Canvas(wrap, highlightthickness=0)
        self.vbar = ttk.Scrollbar(wrap, orient="vertical", command=self.canvas.yview)
        self.canvas.configure(yscrollcommand=self.vbar.set)

        self.list_frame = ttk.Frame(self.canvas)
        self.win_id = self.canvas.create_window((0, 0), window=self.list_frame, anchor="nw")

        self.canvas.grid(row=0, column=0, sticky="nsew")
        self.vbar.grid(row=0, column=1, sticky="ns")  # 必要なら後で消す

        # 1-line status
        ttk.Label(self, textvariable=self.status, anchor="w").grid(row=1, column=0, sticky="ew", pady=(4, 0))

        # Resize/scroll updates
        self.list_frame.bind("<Configure>", lambda e: self._sync_scroll())
        self.canvas.bind("<Configure>", lambda e: (self.canvas.itemconfigure(self.win_id, width=e.width), self._sync_scroll()))

    def _sync_scroll(self):
        self.canvas.configure(scrollregion=self.canvas.bbox("all"))
        bbox = self.canvas.bbox("all")
        if not bbox:
            self.vbar.grid_remove()
            return
        content_h = bbox[3] - bbox[1]
        canvas_h = self.canvas.winfo_height()
        if content_h <= canvas_h + 2:
            self.vbar.grid_remove()
        else:
            self.vbar.grid()

    # ---- Logic ----
    def _load(self):
        try:
            self.api_token, self.devices = load_config(CONFIG)
            self.status.set(f"Loaded {len(self.devices)} devices ({CONFIG.name})")
        except Exception as e:
            messagebox.showerror("Config error", f"{e}")
            self.status.set(f"Config error: {e}")

    def _render(self):
        for w in self.list_frame.winfo_children():
            w.destroy()

        if not self.devices:
            ttk.Label(self.list_frame, text="No devices.").grid(row=0, column=0, padx=8, pady=8, sticky="w")
            return

        # Header
        ttk.Label(self.list_frame, text="Device", width=22).grid(row=0, column=0, sticky="w", padx=(6, 0))
        ttk.Label(self.list_frame, text="Controls").grid(row=0, column=1, sticky="w", padx=(6, 0))

        for i, d in enumerate(self.devices, start=1):
            ttk.Label(self.list_frame, text=d.name, width=22).grid(row=i, column=0, sticky="w", padx=(6, 0), pady=6)

            btns = ttk.Frame(self.list_frame)
            btns.grid(row=i, column=1, sticky="w", padx=(6, 0), pady=6)

            ttk.Button(btns, text="ON",  command=lambda did=d.ble_mac, nm=d.name: self._send(nm, did, "turnOn")).grid(row=0, column=0, padx=2)
            ttk.Button(btns, text="OFF", command=lambda did=d.ble_mac, nm=d.name: self._send(nm, did, "turnOff")).grid(row=0, column=1, padx=2)

        self._sync_scroll()

    def _send(self, name: str, ble_mac: str, command: str):
        if not self.api_token:
            self.status.set("API key missing")
            return

        self.status.set(f"Sending: {command} ({name})")

        def worker():
            try:
                r = post_command(self.api_token, ble_mac, command)
                self.master.after(0, lambda: self.status.set(f"{command} {name} -> {r.status_code}"))
            except Exception as e:
                self.master.after(0, lambda: self.status.set(f"{command} {name} -> ERROR {e}"))

        threading.Thread(target=worker, daemon=True).start()


def main():
    root = tk.Tk()
    # OS標準テーマ優先
    try:
        style = ttk.Style()
        for theme in ("vista", "xpnative", "aqua", "clam"):
            if theme in style.theme_names():
                style.theme_use(theme)
                break
    except Exception:
        pass

    App(root)
    root.mainloop()


if __name__ == "__main__":
    main()
