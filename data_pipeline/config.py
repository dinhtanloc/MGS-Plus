
import os
import yaml
from dotenv import load_dotenv, find_dotenv
from pyprojroot import here

load_dotenv(find_dotenv())

with open(here("configs/data-pipeline-configs.yml")) as cfg:
    app_config = yaml.load(cfg, Loader=yaml.FullLoader)


class LoadDEConffig:
    def __init__(self) -> None:
        self.kaggle_name = os.getenv("KAGGLE_USERNAME")
        self.kaggle_key = os.getenv("KAGGLE_KEY")

        self.kaggle_config = app_config["kaggle"]["config_dir"]

LoadDEConffig = LoadDEConffig()
