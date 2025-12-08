package main

import (
	"net"
	"net/http"
	"os"

	"github.com/charmbracelet/log"
	"github.com/olivere/vite"
	"github.com/spf13/cobra"
	"github.com/unmango/go/cli"
)

var isDev bool

var cmd = &cobra.Command{
	Use:   "wishlists",
	Short: "Wishlists UnstoppableMango style",
	Run: func(cmd *cobra.Command, args []string) {
		v, err := vite.NewHandler(vite.Config{
			FS:    os.DirFS("./src/web"),
			IsDev: isDev,
		})
		if err != nil {
			cli.Fail(err)
		}

		srv := http.NewServeMux()
		srv.Handle("/", v)

		lis, err := net.Listen("tcp", ":8080")
		if err != nil {
			cli.Fail(err)
		}

		log.Info("Serving on :8080")
		if err = http.Serve(lis, srv); err != nil {
			cli.Fail(err)
		}
	},
}

func init() {
	cmd.Flags().BoolVar(&isDev, "dev", true, "Development mode")
}

func main() {
	if err := cmd.Execute(); err != nil {
		cli.Fail(err)
	}
}
