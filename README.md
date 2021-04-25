# UFiber.Configurator

> **Disclaimer**: This **NOT** an official tool nor it was endorsed in any way by Ubiquiti. This is a community driven tool. Even though we will do our best to help the community there are no implicity or explicity warranties whatsoever. Use at your own risk.

FTTx became a quite popular technology to deliver high speed connectivity specially at homes (FTTH) on which on a single fiber optics terminal you can provision and deliver internet access, IPTV and VoIP. 

[Ubiquiti](https://www.ui.com) has a line of fiber optics networking products that target usually ISPs. Among those products, some of them in particular are used on the customer premises as a CPE, which receives the fiber optics line and transform it on ethernet which is usually called GPON CPE or ONT. 

The Ubiquiti products that fall on this category are: [UFiber Loco (UF-Loco)](https://www.ui.com/ufiber/ufiber-loco/), [UFiber Nano G (UF-Nano-G)](https://www.ui.com/ufiber/ufiber-nano-g/), [UFiber Instant (UF-Instant)](https://store.ui.com/collections/operator-ufiber/products/uf-instant). 

Even thought Ubiquiti advertive UF-Loco and UF-Nano-G as to support third party OLT devices on your ISP, the most "clean" usage requires a full UFiber networking deployment on your ISP to be as much as "plug and play" as possible. The UF-Instant is even worse, where Ubiquiti says that it *only* works if you are connected to a fiber optics line which is provided by an UFiber OLT.

On UF-Loco and UF-Nano-G, the 3rd party support allow you to select a profile and set a very limited number of options to make it work with them which not always is enough to make it work on ISPs that have custom or more complex fiber networks.

This tool allow you to overcome those limitations by patching UF-Loco and UF-Nano-G file system to allow those customizations and make it work properly with most of the ISPs.

## Why are you doing that and not using the UFiber admin pages?

After frustrating attempts to provide feedback and ask Ubiquiti for the ability to change some of those configuration using the embedded admin web pages natively, and seeing many users having the same problem, we decided to create this small tool to make it a simple one shot patch for the problem.

Most of us use FTTH and usually the clunky ISP ONT/Modems are pretty bad or provide a lot of limitations then we usually replace this modem with an custom GPON ONT device (like the ones mentioned here) and use our own routers to provide networking for our environments.

## How does it work?

Essentially this is the flow:
1. Connects to your UFiber device using SSH and SCP;
2. Generate a dump of one of its partitions;
3. Pull the dump file to your host computer;
4. Apply the patch with the settings you passed to the tool as parameters (i.e. SLID, Vendor Id, Serial Number, MAC);
5. Push the file to the UFiber device;
6. Write the patched file to the original partition from which the dump was taken at first place.

## Supported UFiber devices

- UF-Loco (firmware 4.3.0)
- UF-Nano-G (firmware 4.3.0)
- UF-Instant *(still under test)*

## Requirements

Unlike other approaches found over the internet, this tool doesn't require any dependencies and is totally self-contained. 

All you need is:

1. A Windows, Linux or MacOS computer;
2. The target UFiber device with a supported firmware (you can download the firmware files from [Ubiquiti downloads page](https://www.ui.com/download/#!ufiber));
3. Have SSH enabled on the target UFiber device.
4. Download the package from the [Releases section](https://github.com/Unifi-Tools/UFiber.Configurator/releases) of this repository for your OS.

## Usage

By running the `UFiber.Configurator --help` you will get all the parameters used by this tool:

```
UFiber.Configurator
  Apply configuration changes to UFiber devices

Usage:
  UFiber.Configurator [options]

Options:
  --host <host>      IP or hostname of the target UFiber device.
  --user <user>      SSH user name. [default: ubnt]
  --pw <pw>          SSH password. [default: ubnt]
  --port <port>      SSH port of the target UFiber device. [default: 22]
  --dry-run          Don't apply the patched file to the target UFiber device. (i.e. dry-run)
  --slid <slid>      The SLID (or PLOAM Password).
  --vendor <vendor>  4-digit Vendor Id (e.g. HWTC, MTSC, etc.). Combined with --serial, a GPON Serial Number is 
                     built.
  --serial <serial>  8-digit serial number (e.g. 01234567). Combined with --vendor, a GPON Serial Number is 
                     built.
  --mac <mac>        The desired MAC address to clone.
  --version          Show version information
  -?, -h, --help     Show help and usage information
```

## Contributions and feedback

Please feel free to open issues and contribute back.

A huge thanks and kudos to [@jakesays](https://github.com/jakesays) for all the help while creating this tool.