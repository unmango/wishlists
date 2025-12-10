package main

import (
	"net"
	"net/http"
	"os"

	"github.com/charmbracelet/log"
	"github.com/olivere/vite"
	"github.com/spf13/cobra"
	"github.com/spf13/viper"
	"github.com/unmango/go/cli"
)

var cmd = &cobra.Command{
	Use:   "wishlists",
	Short: "Wishlists UnstoppableMango style",
	Run: func(cmd *cobra.Command, args []string) {
		assets := viper.GetString("assets")
		log.Info("Assets", "path", assets)

		v, err := vite.NewHandler(vite.Config{
			FS:    os.DirFS(assets),
			IsDev: viper.GetBool("dev"),
		})
		if err != nil {
			cli.Fail(err)
		}

		srv := http.NewServeMux()
		srv.Handle("/", v)

		lis, err := net.Listen("tcp", viper.GetString("listen_address"))
		if err != nil {
			cli.Fail(err)
		}

		log.Info("Serving", "addr", lis.Addr())
		if err = http.Serve(lis, srv); err != nil {
			cli.Fail(err)
		}
	},
}

func init() {
	viper.SetEnvPrefix("wish")
	viper.BindEnv("dev")
	viper.BindEnv("assets")
	viper.BindEnv("listen_address")

	cmd.Flags().Bool("dev", false, "Development mode")
	viper.BindPFlag("dev", cmd.Flags().Lookup("dev"))

	cmd.Flags().String("assets", "/wwwroot", "Static assets path")
	viper.BindPFlag("assets", cmd.Flags().Lookup("assets"))

	cmd.Flags().String("addr", ":8080", "Listen address")
	viper.BindPFlag("listen_address", cmd.Flags().Lookup("addr"))
}

func main() {
	if err := cmd.Execute(); err != nil {
		cli.Fail(err)
	}
}
