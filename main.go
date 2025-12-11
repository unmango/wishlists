package main

import (
	"net"
	"net/http"
	"os"

	"github.com/charmbracelet/log"
	"github.com/go-chi/chi/v5"
	"github.com/olivere/vite"
	"github.com/spf13/cobra"
	"github.com/spf13/viper"
	"github.com/unmango/go/cli"
	"github.com/unmango/wishlists/pkg/api"
	"github.com/unmango/wishlists/pkg/health"
)

var cmd = &cobra.Command{
	Use:   "wishlists",
	Short: "Wishlists UnstoppableMango style",
	Run: func(cmd *cobra.Command, args []string) {
		assets := viper.GetString("assets")
		log.Info("Assets", "path", assets)

		// TODO: This isn't using index.html
		v, err := vite.NewHandler(vite.Config{
			FS:        os.DirFS(assets),
			IsDev:     viper.GetBool("dev"),
			ViteURL:   viper.GetString("vite_url"),
			ViteEntry: "src/web/main.tsx",
		})
		if err != nil {
			cli.Fail(err)
		}

		app := chi.NewRouter()
		app.Handle("/", v)
		app.HandleFunc("/healthz", health.Handler)

		if api, err := api.Router(); err != nil {
			cli.Fail(err)
		} else {
			app.Mount("/api", api)
		}

		port := viper.GetString("port")
		addr := net.JoinHostPort("", port)
		lis, err := net.Listen("tcp", addr)
		if err != nil {
			cli.Fail(err)
		}

		log.Info("Serving", "addr", lis.Addr())
		if err = http.Serve(lis, app); err != nil {
			cli.Fail(err)
		}
	},
}

func init() {
	viper.SetEnvPrefix("wish")
	viper.BindEnv("dev")
	viper.BindEnv("assets")
	viper.BindEnv("port")
	viper.BindEnv("vite_url", "VITE_URL")
	viper.BindEnv("nats", "NATS_URI")

	cmd.Flags().Bool("dev", false, "Development mode")
	viper.BindPFlag("dev", cmd.Flags().Lookup("dev"))

	cmd.Flags().String("assets", "/wwwroot", "Static assets path")
	viper.BindPFlag("assets", cmd.Flags().Lookup("assets"))

	cmd.Flags().String("port", "8080", "Listen port")
	viper.BindPFlag("port", cmd.Flags().Lookup("port"))

	cmd.Flags().String("nats", "", "NATS Uri")
	viper.BindPFlag("nats", cmd.Flags().Lookup("nats"))
}

func main() {
	if err := cmd.Execute(); err != nil {
		cli.Fail(err)
	}
}
