import os

import kagglehub
from pathlib import Path
import shutil

from configs.global_config import exps_dir, data_dir
from data_pipeline.config import LoadDEConffig

class KaggleHubDatasetManager:
    def __init__(self, dataset: str, download_dir: str):
        """
        Args:
            dataset (str): KaggleHub dataset slug
            download_dir (str): target directory
        """
        self.dataset = dataset
        self.download_dir = Path(download_dir)
        os.environ["KAGGLE_CONFIG_DIR"] = LoadDEConffig.kaggle_config

    def download(self, force: bool = False):
        """
        Download dataset using kagglehub
        """
        if self.download_dir.exists() and not force:
            print(f"[INFO] Dataset already exists at {self.download_dir}")
            return

        print(f"[INFO] Downloading {self.dataset} via KaggleHub...")

        path = kagglehub.competition_download(self.dataset)

        if self.download_dir.exists():
            shutil.rmtree(self.download_dir)

        shutil.copytree(path, self.download_dir)

        print(f"[SUCCESS] Dataset available at {self.download_dir}")

    def list_files(self):
        if not self.download_dir.exists():
            print("[WARNING] Dataset not found")
            return []

        return [
            str(p)
            for p in self.download_dir.rglob("*")
            if p.is_file()
        ]

    def delete(self):
        if self.download_dir.exists():
            shutil.rmtree(self.download_dir)
            print(f"[INFO] Deleted dataset at {self.download_dir}")
        else:
            print("[WARNING] Nothing to delete")

if __name__ == "__main__":
    manager = KaggleHubDatasetManager(
        dataset="grand-xray-slam-division-a",
        download_dir=data_dir
    )

    manager.download()

    files = manager.list_files()
    print(files[:5])

    manager.delete()