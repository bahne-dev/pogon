{
  description = "A Nix-flake-based C# development environment";

  inputs.nixpkgs.url = "https://flakehub.com/f/NixOS/nixpkgs/0.1.*.tar.gz";

  outputs =
    { self, nixpkgs }:
    let
      supportedSystems = [
        "x86_64-linux"
        "aarch64-linux"
        "x86_64-darwin"
        "aarch64-darwin"
      ];
      forEachSupportedSystem =
        f:
        nixpkgs.lib.genAttrs supportedSystems (
          system:
          f {
            pkgs = import nixpkgs {
              inherit system;
              config.allowUnfreePredicate =
                pkg:
                builtins.elem (nixpkgs.lib.getName pkg) [
                  "rider"
                  "vintagestory"
                ];
            };
          }
        );
    in
    {
      devShells = forEachSupportedSystem (
        { pkgs }:
        {
          default = pkgs.mkShell {
            packages = with pkgs; [
              #dotnet-sdk_6
              #dotnet-sdk_7
              #dotnet-sdk_8
              dotnet-sdk_10
              omnisharp-roslyn
              mono
              msbuild

              jetbrains.rider

              vintagestory

              # for prototyping
              python3
              inotify-tools

              stdenv.cc.cc.lib
              zlib

              python3Packages.tkinter
            ];

            # the game libs normally come from the vintagestory wrapper, which
            # Rider skips so it can debug (and hot reload) the dotnet process
            LD_LIBRARY_PATH = "${pkgs.lib.makeLibraryPath (
              [
                pkgs.stdenv.cc.cc.lib
                pkgs.zlib
              ]
              ++ pkgs.vintagestory.passthru.runtimeLibs
            )}";

            VINTAGE_STORY = "${pkgs.vintagestory}/share/vintagestory";
            VINTAGE_STORY_BIN = "${pkgs.vintagestory}/bin/vintagestory";
            VINTAGE_STORY_DOTNET = "${pkgs.dotnet-sdk_10}/share/dotnet/dotnet";


            shellHook = ''
              if [ -f .env ]; then
                source .env
              fi

              if [ -z "$NuGetPackageRoot" ]; then
                echo "WARNING: NuGetPackageRoot not set!"
                echo "The Bepu physics DLLs may not be found, and the mod may fail to load."
                echo "Add this to .env:"
                echo '  export NuGetPackageRoot="$HOME/.nuget/packages/"'
              fi

              alias bld="./build.sh"
              alias dbg="./debug.sh"
              alias wt="./watch.sh"
              alias prt="source ./venv/bin/activate && pip install -r requirements.txt"

              echo bld="./build.sh"
              echo dbg="./debug.sh"
              echo wt="./watch.sh"
              echo prt="source ./venv/bin/activate && pip install -r requirements.txt"
            '';
          };
        }
      );
    };
}
