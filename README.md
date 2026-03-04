# CarbonFiles CLI (`cf`)

A colorful command-line tool for managing CarbonFiles -- buckets, files, API keys, tokens, and more. Built with [Spectre.Console](https://spectreconsole.net/) for rich terminal output.

## Installation

### dotnet tool (requires .NET 10)

```bash
dotnet tool install -g CarbonFiles.Cli
```

### From source

```bash
git clone <repo-url>
cd carbon-cli
dotnet build
```

## Quick Start

```bash
# Configure your server
cf config set --url https://files.example.com --token cf4_your_api_key

# Create a bucket
cf bucket create "My Files" -e 7d

# Upload files
cf file upload <bucket-id> ./document.pdf ./image.png

# List your buckets
cf bucket list

# Download a file
cf file download <bucket-id> document.pdf
```

## Command Reference

| Command | Description |
|---------|-------------|
| **Configuration** | |
| `cf config set --url <url> --token <token>` | Configure server connection |
| `cf config show` | Show current profile configuration |
| `cf config profiles` | List all saved profiles |
| `cf config use <name>` | Switch active profile |
| **Buckets** | |
| `cf bucket list` | List all buckets |
| `cf bucket create <name>` | Create a new bucket |
| `cf bucket info <id>` | Show bucket details |
| `cf bucket update <id>` | Update a bucket |
| `cf bucket delete <id>` | Delete a bucket |
| `cf bucket download <id>` | Download bucket as ZIP |
| `cf bucket watch <id>` | Watch bucket for live changes via SignalR |
| **Files** | |
| `cf file list <bucket-id>` | List files in a bucket |
| `cf file info <bucket-id> <path>` | Show file details |
| `cf file upload <bucket-id> [paths]` | Upload files to a bucket |
| `cf file download <bucket-id> <path>` | Download a file from a bucket |
| `cf file delete <bucket-id> <path>` | Delete a file |
| **API Keys** | |
| `cf key list` | List all API keys |
| `cf key create <name>` | Create a new API key |
| `cf key delete <prefix>` | Revoke an API key |
| `cf key usage <prefix>` | Show API key usage stats |
| **Tokens** | |
| `cf token create upload <bucket-id>` | Create an upload token for a bucket |
| `cf token create dashboard` | Create a dashboard token |
| `cf token info` | Show current dashboard token info |
| **Short URLs** | |
| `cf short resolve <code>` | Resolve a short URL code |
| `cf short delete <code>` | Delete a short URL |
| **System** | |
| `cf stats` | Show system-wide statistics |
| `cf health` | Check API health status |

## Configuration

The CLI stores its configuration in `~/.cf/config.json`. Configuration is organized into named profiles, allowing you to connect to multiple CarbonFiles servers or use different credentials.

### Setting up a profile

```bash
# Configure the default profile
cf config set --url https://files.example.com --token cf4_your_api_key

# Create a named profile
cf config set --profile staging --url https://staging.example.com --token cf4_staging_key
```

### Managing profiles

```bash
# List all profiles
cf config profiles

# Switch the active profile
cf config use staging

# Show current profile configuration
cf config show
```

### Per-command profile override

Any command accepts the `--profile` flag to use a specific profile without switching the active one:

```bash
cf bucket list --profile staging
```

## Global Options

| Flag | Description |
|------|-------------|
| `--json` | Output raw JSON instead of formatted tables (useful for piping to `jq`) |
| `--profile <name>` | Use a specific config profile for this command |
| `--help` | Show help information |
| `--version` | Show version information |

## Examples

### Create a bucket with an expiry

```bash
cf bucket create "Sprint Demo" -d "Files for the sprint review" -e 7d
```

### Upload a directory recursively

```bash
cf file upload abc123 ./build-output/ -r
```

### Pipe content from stdin

```bash
echo "Hello, world!" | cf file upload abc123 --stdin -n greeting.txt
```

### Use JSON output with jq

```bash
# Get all bucket IDs
cf bucket list --json | jq '.[].id'

# Get the total size of a bucket
cf bucket info abc123 --json | jq '.total_size'
```

### Create an upload token and use it

```bash
# Create a token that allows 10 uploads and expires in 1 hour
cf token create upload abc123 -e 1h --max-uploads 10

# Use the token to upload (instead of your API key)
cf file upload abc123 ./photo.jpg --token cfu_the_upload_token
```

### Watch a bucket for live changes

```bash
cf bucket watch abc123
```

This opens a live SignalR connection and streams file created, updated, and deleted events as they happen. Press `Ctrl+C` to stop.

### Download a file to a specific path

```bash
cf file download abc123 reports/q4.pdf -o ~/Downloads/q4-report.pdf
```

### Download an entire bucket as a ZIP

```bash
cf bucket download abc123
```

## Development

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run a CLI command during development
dotnet run --project src/CarbonFiles.Cli -- <command>
```
