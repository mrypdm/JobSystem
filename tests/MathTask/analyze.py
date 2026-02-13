import pandas as pd
import matplotlib.pyplot as plt
import os

dir_path = os.path.dirname(os.path.realpath(__file__))
res_path = f"{dir_path}/bin/Debug/net9.0/results"

cpu_contraints = [
    576,
    864,
    1024,
]
ram_contraints = [
    128,
    192,
    256,
    512,
    1024,
]

def get_quantile(df, quantile, name):
    quant = df[name].quantile(quantile)
    print(f"\tQuantile ({quantile}) of {name} is {quant}")
    return [quant] * len(df)


def build_graph(title, path, x_vars, x_name, y_vars_collection):
    plt.figure(figsize=(12, 12))
    plt.title(title)
    for y_name, y_vars in y_vars_collection:
        plt.plot(
            x_vars,
            y_vars,
            label=y_name,
        )
        plt.xlabel(x_name)

    plt.legend(loc="upper left")
    plt.savefig(path, dpi=400, transparent=False)
    plt.close()


# Analyzing execution metrics (resources + queue of Jobs)
def metrics(sample, cpu, ram):
    print(f"[Metrics] Sample: {sample} CPU: {cpu} RAM: {ram}")

    metrics = pd.read_csv(
        f"{res_path}/{sample}/{cpu}_{ram}.metrics.csv",
        sep=";",
    )
    metrics["cpu"] = metrics["cpu"] / 100   # Cores
    metrics["ram"] = metrics["ram"] / 2**30 # GB
    metrics["queue"] = metrics["total"] - metrics["run"]
    metrics["queue_ratio"] = metrics["queue"] / metrics["total"] * 100

    cpu_quantile = get_quantile(metrics, 0.9, "cpu")
    ram_quantile = get_quantile(metrics, 0.9, "ram")
    queue_quantile = get_quantile(metrics, 0.9, "queue")
    queue_ratio_quantile = get_quantile(metrics, 0.9, "queue_ratio")

    build_graph(
        "Resources", f"{res_path}/{sample}/{cpu}_{ram}.metrics.resources.png",
        metrics["time"],
        "Time, ms",
        [
            ("CPU, Cores", metrics["cpu"]),
            ("CPU (90%), Cores", cpu_quantile),
            ("RAM, GB", metrics["ram"]),
            ("RAM (90%), GB", ram_quantile),
        ]
    )

    build_graph(
        "Jobs", f"{res_path}/{sample}/{cpu}_{ram}.metrics.jobs.png",
        metrics["time"],
        "Time, ms",
        [
            ("Total", metrics["total"]),
            ("Running", metrics["run"]),
            ("Queue (90%)", queue_quantile),
            ("Queue ratio (90%), %", queue_ratio_quantile),
        ]
    )

    print ()


def get_group_time_statistics(group):
    raw_group = group.values
    waiting_time = raw_group[1][0] - raw_group[0][0]
    execution_time = raw_group[2][0] - raw_group[1][0]

    return pd.Series({
        'waiting_time': waiting_time / 1000,
        'execution_time': execution_time / 1000,
        'wait_ratio': waiting_time / execution_time * 100
    })


# Analyzing timeline metrics (waiting time, execution time, etc.)
def timeline(sample, cpu, ram):
    print(f"[Timeline] Sample: {sample} CPU: {cpu} RAM: {ram}")

    timeline = pd.read_csv(
        f"{res_path}/{sample}/{cpu}_{ram}.timeline.csv",
        sep=";",
    )
    grouped = timeline.groupby(["job_id"]).apply(get_group_time_statistics).reset_index()

    wait_time_quantile = get_quantile(grouped, 0.9, "waiting_time")
    wait_ratio_quantile = get_quantile(grouped, 0.9, "wait_ratio")

    build_graph(
        "Jobs", f"{res_path}/{sample}/{cpu}_{ram}.timeline.wait-time.png",
        grouped["job_id"],
        "Job, ID",
        [
            ("Waiting time, s", grouped["waiting_time"]),
            ("Waiting time (90%), s", wait_time_quantile),
            ("Wait-to-execute ratio (90%), %", wait_ratio_quantile),
        ]
    )

    print ()


# TODO: use loop
# TODO: add more CPU/RAM configurations (more RAM steps, RAM+CPU > 1024)
# TODO: add comparing between samples and between CPU/RAM configurations
#       can do it by: analyzing smaples in loop and saving results of analysis to another csv file

timeline(0, 576, 512)
metrics(0, 576, 512)
timeline(0, 576, 1024)
metrics(0, 576, 1024)

timeline(0, 864, 512)
metrics(0, 864, 512)
timeline(0, 864, 1024)
metrics(0, 864, 1024)

timeline(0, 1024, 512)
metrics(0, 1024, 512)
timeline(0, 1024, 1024)
metrics(0, 1024, 1024)
